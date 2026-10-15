using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using TouchRpc.Core;
using TouchRpc.Infrastructure;

namespace TouchRpc.App;

internal sealed class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly AppSettingsStore _settingsStore = new();
    private readonly TouchProcessDetector _processDetector = new();
    private DiscordPresenceService? _discord;
    private CancellationTokenSource? _monitorCancellation;
    private MonitoringSession? _session;
    private string _logPath = string.Empty;
    private string _localRoleId = string.Empty;
    private string _manualNickname = string.Empty;
    private string? _manualNicknameRoleId;
    private bool _manualNicknameAwaitingBinding;
    private LocalRole _configuredRole;
    private TouchProcessCandidate? _selectedProcess;
    private bool _followProcessLifecycle;
    private bool _isMonitoring;
    private string _sourceHealth = "Not configured";
    private string _sourceDiagnostic = "Detect TouchClient or browse to output_log.txt.";
    private string _details = "No preview";
    private string _state = string.Empty;
    private string _phase = "Unknown";
    private string _roleEvidence = "Unknown";
    private string _roster = "Unknown";
    private string _diagnostic = "None";
    private string _afk = "Not tracking";
    private string _nickname = "TOUCH · GeneralFallback · AutomaticUnavailable";
    private string _discordApplicationId = string.Empty;
    private string _discordLargeImageKey = string.Empty;
    private string _discordStatus = "Disabled";
    private int _afkThresholdMinutes = 30;

    public ObservableCollection<TouchProcessCandidate> ProcessCandidates { get; } = [];

    public IReadOnlyList<LocalRole> ConfigurableRoles { get; } =
        [LocalRole.Unknown, LocalRole.Joined, LocalRole.Spectator];

    public string LogPath
    {
        get => _logPath;
        set => SetProperty(ref _logPath, value);
    }

    public string LocalRoleId
    {
        get => _localRoleId;
        set => SetProperty(ref _localRoleId, value);
    }

    public string ManualNickname
    {
        get => _manualNickname;
        set
        {
            if (!SetProperty(ref _manualNickname, value)) return;
            _manualNicknameRoleId = null;
            _manualNicknameAwaitingBinding = !string.IsNullOrWhiteSpace(value);
        }
    }

    public string DiscordApplicationId
    {
        get => _discordApplicationId;
        set => SetProperty(ref _discordApplicationId, value);
    }

    public string DiscordLargeImageKey
    {
        get => _discordLargeImageKey;
        set => SetProperty(ref _discordLargeImageKey, value);
    }

    public LocalRole ConfiguredRole
    {
        get => _configuredRole;
        set => SetProperty(ref _configuredRole, value);
    }

    public TouchProcessCandidate? SelectedProcess
    {
        get => _selectedProcess;
        set
        {
            if (!SetProperty(ref _selectedProcess, value) || value is null) return;
            if (value.LogPath is not null)
            {
                LogPath = value.LogPath;
                SourceDiagnostic = "A log path was derived from the selected TouchClient process.";
            }
            else
            {
                SourceDiagnostic = "The process is running, but its executable path is unavailable. Use Browse.";
            }
        }
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set
        {
            if (SetProperty(ref _isMonitoring, value))
            {
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
            }
        }
    }

    public bool CanStart => !IsMonitoring;
    public bool CanStop => IsMonitoring;
    public string SourceHealth { get => _sourceHealth; private set => SetProperty(ref _sourceHealth, value); }
    public string SourceDiagnostic { get => _sourceDiagnostic; private set => SetProperty(ref _sourceDiagnostic, value); }
    public string Details { get => _details; private set => SetProperty(ref _details, value); }
    public string State { get => _state; private set => SetProperty(ref _state, value); }
    public string Phase { get => _phase; private set => SetProperty(ref _phase, value); }
    public string RoleEvidence { get => _roleEvidence; private set => SetProperty(ref _roleEvidence, value); }
    public string Roster { get => _roster; private set => SetProperty(ref _roster, value); }
    public string Diagnostic { get => _diagnostic; private set => SetProperty(ref _diagnostic, value); }
    public string Afk { get => _afk; private set => SetProperty(ref _afk, value); }
    public string Nickname { get => _nickname; private set => SetProperty(ref _nickname, value); }
    public string DiscordStatus { get => _discordStatus; private set => SetProperty(ref _discordStatus, value); }

    public async Task InitializeAsync()
    {
        var settings = await _settingsStore.LoadAsync();
        LogPath = settings.LogPath ?? string.Empty;
        LocalRoleId = settings.LocalRoleId ?? string.Empty;
        ConfiguredRole = settings.ConfiguredRole;
        _afkThresholdMinutes = settings.AfkThresholdMinutes;
        _manualNickname = settings.ManualNickname ?? string.Empty;
        _manualNicknameRoleId = settings.ManualNicknameRoleId;
        DiscordApplicationId = settings.DiscordApplicationId ?? string.Empty;
        DiscordLargeImageKey = settings.DiscordLargeImageKey ?? string.Empty;
        _manualNicknameAwaitingBinding = false;
        OnPropertyChanged(nameof(ManualNickname));
        ConfigureDiscord();
        DetectProcesses();
    }

    public void DetectProcesses()
    {
        ProcessCandidates.Clear();
        foreach (var candidate in _processDetector.FindCandidates()) ProcessCandidates.Add(candidate);

        if (ProcessCandidates.Count == 0)
        {
            SelectedProcess = null;
            SourceHealth = LogSourceHealth.GameClosed.ToString();
            SourceDiagnostic = "TouchClient.exe was not found. You can still browse to a log for diagnostics.";
        }
        else if (ProcessCandidates.Count == 1)
        {
            SelectedProcess = ProcessCandidates[0];
        }
        else
        {
            SelectedProcess = null;
            SourceDiagnostic = "Multiple TouchClient processes were found. Select one explicitly.";
        }
    }

    public async Task<bool> StartAsync()
    {
        var validation = LogPathValidator.Validate(LogPath);
        SourceHealth = validation.Health.ToString();
        SourceDiagnostic = validation.Message;
        if (!validation.IsValid) return false;

        var roleId = string.IsNullOrWhiteSpace(LocalRoleId) ? null : LocalRoleId.Trim();
        if (roleId is not null && !RoleId.IsValid(roleId))
        {
            SourceHealth = LogSourceHealth.Unreadable.ToString();
            SourceDiagnostic = "Character Role ID must contain digits only. Launcher userid is not accepted.";
            return false;
        }

        await SaveSettingsAsync(roleId);
        ConfigureDiscord();
        _session = new MonitoringSession(
            LogPath.Trim(),
            roleId,
            ConfiguredRole,
            afkThreshold: TimeSpan.FromMinutes(_afkThresholdMinutes),
            manualNickname: string.IsNullOrWhiteSpace(ManualNickname) ? null : ManualNickname,
            manualNicknameRoleId: _manualNicknameRoleId);
        _monitorCancellation = new CancellationTokenSource();
        _followProcessLifecycle = SelectedProcess is not null;
        IsMonitoring = true;
        _ = MonitorLoopAsync(_monitorCancellation.Token);
        return true;
    }

    public void Stop()
    {
        _monitorCancellation?.Cancel();
        _monitorCancellation?.Dispose();
        _monitorCancellation = null;
        _session = null;
        _followProcessLifecycle = false;
        IsMonitoring = false;
        _discord?.Clear();
        SourceDiagnostic = "Monitoring stopped. Preview retained for inspection.";
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        if (_discord is not null)
        {
            _discord.StatusChanged -= Discord_StatusChanged;
            _discord.Dispose();
            _discord = null;
        }
        await Task.CompletedTask;
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _session is not null)
            {
                if (_followProcessLifecycle && SelectedProcess is not null && !IsProcessRunning(SelectedProcess.ProcessId))
                {
                    _ = ApplySnapshot(_session.ClearForGameClosed());
                    _discord?.Clear();
                    SelectedProcess = null;
                }

                if (_followProcessLifecycle && SelectedProcess is null)
                {
                    var candidates = _processDetector.FindCandidates();
                    if (candidates.Count == 1)
                    {
                        SelectedProcess = candidates[0];
                        var roleId = string.IsNullOrWhiteSpace(LocalRoleId) ? null : LocalRoleId.Trim();
                        _session = new MonitoringSession(
                            LogPath.Trim(),
                            roleId,
                            ConfiguredRole,
                            afkThreshold: TimeSpan.FromMinutes(_afkThresholdMinutes),
                            manualNickname: string.IsNullOrWhiteSpace(ManualNickname) ? null : ManualNickname,
                            manualNicknameRoleId: _manualNicknameRoleId);
                        SourceDiagnostic = "TouchClient restarted; monitoring opened a new session.";
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        continue;
                    }
                }

                var snapshot = await _session.PollAsync(cancellationToken);
                if (ApplySnapshot(snapshot)) await SaveSettingsAsync(snapshot.LocalRoleId);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SourceHealth = LogSourceHealth.Unreadable.ToString();
            SourceDiagnostic = "Monitoring stopped because the log became unavailable.";
            Stop();
        }
    }

    private bool ApplySnapshot(MonitoringSnapshot snapshot)
    {
        SourceHealth = snapshot.SourceHealth.ToString();
        SourceDiagnostic = snapshot.SourceDiagnostic;
        Details = snapshot.Presence.Details;
        State = snapshot.Presence.State;
        Phase = snapshot.Phase.ToString();
        RoleEvidence = $"{snapshot.LocalRole} ({snapshot.RoleEvidence})";
        Roster = snapshot.MemberCount is int count
            ? $"{snapshot.RosterCompleteness} · {count} unique member(s)"
            : snapshot.RosterCompleteness.ToString();
        Diagnostic = snapshot.Presence.Diagnostic.ToString();
        Afk = snapshot.IsAfkTracking
            ? $"{(snapshot.IsAfk ? "AFK" : "Counting")} · {FormatElapsed(snapshot.AfkElapsed)} · {snapshot.AfkTimerOrigin} · threshold {_afkThresholdMinutes}m"
            : "Not tracking";
        Nickname = $"Name: {snapshot.LocalNickname ?? "Unavailable"} · Source: {snapshot.NicknameSource} · " +
            $"Tooltip: {snapshot.NicknameTooltip} · {snapshot.NicknameDiagnostic}";
        if (snapshot.SourceHealth != LogSourceHealth.GameClosed)
        {
            _discord?.Publish(new DiscordPresencePayload(
                snapshot.Presence.Details,
                string.IsNullOrWhiteSpace(snapshot.Presence.State) ? null : snapshot.Presence.State,
                string.IsNullOrWhiteSpace(DiscordLargeImageKey) ? null : DiscordLargeImageKey.Trim(),
                snapshot.NicknameTooltip));
        }
        if (_manualNicknameAwaitingBinding && !string.IsNullOrWhiteSpace(ManualNickname) && snapshot.LocalRoleId is not null)
        {
            _manualNicknameRoleId = snapshot.LocalRoleId;
            _manualNicknameAwaitingBinding = false;
            return true;
        }
        return false;
    }

    private Task SaveSettingsAsync(string? roleId) => _settingsStore.SaveAsync(new AppSettings(
        LogPath.Trim(),
        roleId,
        ConfiguredRole,
        _afkThresholdMinutes,
        string.IsNullOrWhiteSpace(ManualNickname) ? null : ManualNickname,
        _manualNicknameRoleId,
        DiscordApplicationId,
        DiscordLargeImageKey));

    private void ConfigureDiscord()
    {
        if (_discord is not null)
        {
            _discord.StatusChanged -= Discord_StatusChanged;
            _discord.Dispose();
        }

        var applicationId = AppSettingsStore.NormalizeDiscordApplicationId(DiscordApplicationId);
        _discord = new DiscordPresenceService(applicationId);
        _discord.StatusChanged += Discord_StatusChanged;
        DiscordStatus = $"{_discord.State} · {_discord.Diagnostic}";
        if (applicationId is null && !string.IsNullOrWhiteSpace(DiscordApplicationId))
            DiscordStatus = "Error · Application ID must contain digits only.";
    }

    private void Discord_StatusChanged(object? sender, DiscordStatusChangedEventArgs eventArgs)
    {
        System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            DiscordStatus = $"{eventArgs.State} · {eventArgs.Diagnostic}");
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string FormatElapsed(TimeSpan elapsed) =>
        $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
}