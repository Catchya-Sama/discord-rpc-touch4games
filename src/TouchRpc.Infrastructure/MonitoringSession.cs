using TouchRpc.Core;

namespace TouchRpc.Infrastructure;

public sealed record MonitoringSnapshot(
    LogSourceHealth SourceHealth,
    string SourceDiagnostic,
    PresenceView Presence,
    SessionPhase Phase,
    LocalRole LocalRole,
    EvidenceKind RoleEvidence,
    RosterCompleteness RosterCompleteness,
    int? MemberCount,
    bool IsAfkTracking,
    bool IsAfk,
    TimeSpan AfkElapsed,
    AfkTimerOrigin AfkTimerOrigin,
    bool NewSession,
    int ProcessedLines);

public sealed class MonitoringSession
{
    private readonly LiveLogReader _reader;
    private readonly TouchLogParser _parser = new();
    private readonly string? _localRoleId;
    private readonly LocalRole _configuredRole;
    private readonly IMonotonicClock _clock;
    private readonly TimeSpan _afkThreshold;
    private TouchStateReducer _reducer;

    public MonitoringSession(
        string logPath,
        string? localRoleId = null,
        LocalRole configuredRole = LocalRole.Unknown,
        IMonotonicClock? clock = null,
        TimeSpan? afkThreshold = null)
    {
        _reader = new LiveLogReader(logPath);
        _localRoleId = localRoleId;
        _configuredRole = configuredRole;
        _clock = clock ?? SystemMonotonicClock.Instance;
        _afkThreshold = afkThreshold ?? AfkPolicy.DefaultThreshold;
        _reducer = CreateReducer();
    }

    public async Task<MonitoringSnapshot> PollAsync(CancellationToken cancellationToken = default)
    {
        var batch = await _reader.ReadAvailableAsync(cancellationToken).ConfigureAwait(false);
        if (batch.NewSession)
        {
            _reducer = CreateReducer();
            _reducer.BeginHistoricalReplay();
        }

        foreach (var line in batch.Lines)
            _reducer.Replay(_parser.ParseLine(line));

        if (batch.NewSession) _reducer.EndHistoricalReplay();
        else _reducer.Tick();

        var state = _reducer.State;
        var presence = PresenceFormatter.Format(state);
        return new MonitoringSnapshot(
            batch.Health,
            batch.Diagnostic,
            presence,
            state.Phase,
            state.LocalRole,
            state.RoleEvidence,
            state.RosterCompleteness,
            state.RosterCompleteness == RosterCompleteness.Complete && state.UniqueMemberCount > 0
                ? state.UniqueMemberCount
                : null,
            state.IsAfkTracking,
            state.IsAfk,
            state.AfkElapsed,
            state.AfkTimerOrigin,
            batch.NewSession,
            batch.Lines.Count);
    }

    public MonitoringSnapshot ClearForGameClosed()
    {
        _reducer = CreateReducer();
        return new MonitoringSnapshot(
            LogSourceHealth.GameClosed,
            "TouchClient is not running; the preview was cleared.",
            new PresenceView("No active game", string.Empty, DiagnosticCode.None),
            SessionPhase.Unknown,
            LocalRole.Unknown,
            EvidenceKind.Unknown,
            RosterCompleteness.Unknown,
            null,
            false,
            false,
            TimeSpan.Zero,
            AfkTimerOrigin.None,
            true,
            0);
    }

    private TouchStateReducer CreateReducer() => new(_localRoleId, _configuredRole, _clock, _afkThreshold);
}