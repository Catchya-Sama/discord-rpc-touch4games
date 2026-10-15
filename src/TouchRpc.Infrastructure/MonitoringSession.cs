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
    bool NewSession,
    int ProcessedLines);

public sealed class MonitoringSession
{
    private readonly LiveLogReader _reader;
    private readonly TouchLogParser _parser = new();
    private readonly string? _localRoleId;
    private readonly LocalRole _configuredRole;
    private TouchStateReducer _reducer;

    public MonitoringSession(string logPath, string? localRoleId = null, LocalRole configuredRole = LocalRole.Unknown)
    {
        _reader = new LiveLogReader(logPath);
        _localRoleId = localRoleId;
        _configuredRole = configuredRole;
        _reducer = CreateReducer();
    }

    public async Task<MonitoringSnapshot> PollAsync(CancellationToken cancellationToken = default)
    {
        var batch = await _reader.ReadAvailableAsync(cancellationToken).ConfigureAwait(false);
        if (batch.NewSession) _reducer = CreateReducer();

        foreach (var line in batch.Lines)
            _reducer.Replay(_parser.ParseLine(line));

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
            true,
            0);
    }

    private TouchStateReducer CreateReducer() => new(_localRoleId, _configuredRole);
}