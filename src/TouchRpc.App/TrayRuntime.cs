using System.IO;
using TouchRpc.Core;
using TouchRpc.Infrastructure;

namespace TouchRpc.App;

internal sealed class TrayRuntime : IAsyncDisposable
{
    private static readonly TimeSpan DiscoveryInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);
    private readonly AppSettingsStore _settingsStore = new();
    private readonly TouchProcessDetector _processDetector = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private DiscordPresenceService? _discord;
    private MonitoringSession? _session;
    private CancellationTokenSource? _cancellation;
    private Task? _worker;
    private int? _processId;
    private bool _disposed;

    public event EventHandler<string>? StatusChanged;

    public bool IsEnabled { get; private set; } = true;

    public async Task InitializeAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var settings = await _settingsStore.LoadAsync();
            IsEnabled = settings.Enabled;
            await _settingsStore.SaveAsync(new AppSettings(Enabled: IsEnabled));
            if (IsEnabled) StartWorker();
            else SetStatus("Disabled");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsEnabled == enabled) return;
            await _settingsStore.SaveAsync(new AppSettings(Enabled: enabled));
            IsEnabled = enabled;
            if (enabled) StartWorker();
            else await StopWorkerAsync("Disabled");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private void StartWorker()
    {
        if (_worker is not null) return;
        _discord = new DiscordPresenceService(ProductConfiguration.DiscordApplicationId);
        _discord.StatusChanged += Discord_StatusChanged;
        _cancellation = new CancellationTokenSource();
        _worker = RunWorkerAsync(_cancellation.Token);
        SetStatus("Waiting for TOUCH");
    }

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                _discord?.Clear();
                _session = null;
                _processId = null;
                SetStatus("Recovering");
                await Task.Delay(DiscoveryInterval, cancellationToken);
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_session is null)
            {
                var detectedProcesses = _processDetector.FindCandidates();
                if (detectedProcesses.Count > 1)
                {
                    SetStatus("Multiple TOUCH clients");
                    await Task.Delay(DiscoveryInterval, cancellationToken);
                    continue;
                }

                var candidates = detectedProcesses
                    .Where(item => item.IsAccessible && item.LogPath is not null)
                    .ToArray();
                if (candidates.Length == 0)
                {
                    SetStatus("Waiting for TOUCH");
                    await Task.Delay(DiscoveryInterval, cancellationToken);
                    continue;
                }
                var candidate = candidates[0];

                var validation = LogPathValidator.Validate(candidate.LogPath!);
                if (!validation.IsValid)
                {
                    SetStatus("Waiting for game log");
                    await Task.Delay(DiscoveryInterval, cancellationToken);
                    continue;
                }

                _processId = candidate.ProcessId;
                _session = new MonitoringSession(candidate.LogPath!);
                SetStatus("Enabled");
            }

            if (_processId is not int processId || !TouchProcessDetector.IsRunning(processId))
            {
                _discord?.Clear();
                _session = null;
                _processId = null;
                SetStatus("Waiting for TOUCH");
                continue;
            }

            try
            {
                var snapshot = await _session.PollAsync(cancellationToken);
                if (snapshot.SourceHealth == LogSourceHealth.Unreadable)
                {
                    _discord?.Clear();
                    _session = null;
                    _processId = null;
                    SetStatus("Waiting for game log");
                    continue;
                }

                _discord?.Publish(new DiscordPresencePayload(
                    snapshot.Presence.Details,
                    string.IsNullOrWhiteSpace(snapshot.Presence.State) ? null : snapshot.Presence.State,
                    ProductConfiguration.DiscordLargeImageKey,
                    snapshot.NicknameTooltip));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _discord?.Clear();
                _session = null;
                _processId = null;
                SetStatus("Waiting for game log");
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private async Task StopWorkerAsync(string status)
    {
        _cancellation?.Cancel();
        if (_worker is not null)
        {
            try { await _worker; }
            catch (OperationCanceledException) { }
        }

        _worker = null;
        _cancellation?.Dispose();
        _cancellation = null;
        _session = null;
        _processId = null;
        if (_discord is not null)
        {
            _discord.StatusChanged -= Discord_StatusChanged;
            _discord.Dispose();
            _discord = null;
        }
        SetStatus(status);
    }

    private void Discord_StatusChanged(object? sender, DiscordStatusChangedEventArgs e)
    {
        SetStatus(e.State switch
        {
            DiscordConnectionState.Connected when _session is not null => "Enabled",
            DiscordConnectionState.Connected => "Waiting for TOUCH",
            DiscordConnectionState.Disconnected or DiscordConnectionState.Error => "Waiting for Discord",
            _ => "Connecting to Discord",
        });
    }

    private void SetStatus(string status) => StatusChanged?.Invoke(this, status);

    public async ValueTask DisposeAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            if (_disposed) return;
            _disposed = true;
            await StopWorkerAsync("Stopped");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }
}