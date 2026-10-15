namespace TouchRpc.Core;

public enum SessionPhase { Unknown, Lobby, Waiting, Loading, Playing, Results }
public enum LocalRole { Unknown, Joined, Spectator }
public enum RosterCompleteness { Unknown, Partial, Complete }
public enum EvidenceKind { Observed, UserConfigured, Inferred, Unknown }
public enum MemberEvidence { Snapshot, RoomMember, AddPlayer }
public enum AfkTimerOrigin { None, RoomEntryObserved, MonitoringStarted }

public enum DiagnosticCode
{
    None,
    RoomEntryPending,
    RosterIncomplete,
    LocalRoleUnknown,
    RoleSwitchAwaitingConfirmation,
    AfkHistoryUnavailable,
    AfkRoleUnknown,
}

public sealed record PresenceView(string Details, string State, DiagnosticCode Diagnostic);