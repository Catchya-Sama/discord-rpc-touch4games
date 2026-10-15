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
    private CancellationTokenSource? _monitorCancellation;
    private MonitoringSession? _session;
    private string _logPath = string.Empty;
    private string _localRoleId = string.Empty;
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
    public string DiscordStatus => "Disabled (P4)";

    public async Task InitializeAsync()
    {
        var settings = await _settingsStore.LoadAsync();
        LogPath = settings.LogPath ?? string.Empty;
        LocalRoleId = settings.LocalRoleId ?? string.Empty;
        ConfiguredRole = settings.ConfiguredRole;
        _afkThresholdMinutes = settings.AfkThresholdMinutes;
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

        await _settingsStore.SaveAsync(new AppSettings(LogPath.Trim(), roleId, ConfiguredRole, _afkThresholdMinutes));
        _session = new MonitoringSession(
            LogPath.Trim(),
            roleId,
            ConfiguredRole,
            afkThreshold: TimeSpan.FromMinutes(_afkThresholdMinutes));
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
        SourceDiagnostic = "Monitoring stopped. Preview retained for inspection.";
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
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
                    ApplySnapshot(_session.ClearForGameClosed());
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
                            afkThreshold: TimeSpan.FromMinutes(_afkThresholdMinutes));
                        SourceDiagnostic = "TouchClient restarted; monitoring opened a new session.";
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        continue;
                    }
                }

                ApplySnapshot(await _session.PollAsync(cancellationToken));
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

    private void ApplySnapshot(MonitoringSnapshot snapshot)
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