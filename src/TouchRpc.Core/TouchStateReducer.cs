namespace TouchRpc.Core;

public sealed class TouchStateReducer
{
    private readonly string? _localRoleId;
    private readonly LocalRole _configuredRole;

    public TouchStateReducer(string? localRoleId = null, LocalRole configuredRole = LocalRole.Unknown)
    {
        if (localRoleId is not null && !RoleId.IsValid(localRoleId))
            throw new ArgumentException("Local role ID must contain digits only.", nameof(localRoleId));

        _localRoleId = localRoleId;
        _configuredRole = configuredRole;
        State = new TouchState();
        if (configuredRole != LocalRole.Unknown)
        {
            State.LocalRole = configuredRole;
            State.RoleEvidence = EvidenceKind.UserConfigured;
        }
    }

    public TouchState State { get; }

    public TouchState Apply(LogEvent logEvent)
    {
        switch (logEvent)
        {
            case RoomEntryRequested: BeginRoomEntry(); break;
            case RoomDetailsStarted: StartRoomDetails(); break;
            case MemberObserved member: State.AddMember(member.RoleId); break;
            case MemberLeft leave: ApplyMemberLeave(leave.RoleId); break;
            case MidgameEntryObserved: State.MidgameEntrySeen = true; break;
            case WorldObserved world: ApplyWorld(world.Phase); break;
            case GameplayObserved: State.Phase = SessionPhase.Playing; break;
            case ResultsObserved: State.Phase = SessionPhase.Results; break;
            case RoleSwitchRequested:
                State.IsRoleSwitchPending = true;
                break;
            case RoomOperationObserved operation: ApplyRoomOperation(operation); break;
        }

        RefreshDiagnostic();
        return State;
    }

    public TouchState Replay(IEnumerable<LogEvent> events)
    {
        foreach (var logEvent in events) Apply(logEvent);
        return State;
    }

    private void BeginRoomEntry()
    {
        State.ClearMembers();
        State.Phase = SessionPhase.Unknown;
        State.LocalRole = _configuredRole;
        State.RoleEvidence = _configuredRole == LocalRole.Unknown
            ? EvidenceKind.Unknown
            : EvidenceKind.UserConfigured;
        State.RosterCompleteness = RosterCompleteness.Partial;
        State.IsRoomEntryPending = true;
        State.IsRoleSwitchPending = false;
        State.RoomDetailsSeen = false;
        State.MidgameEntrySeen = false;
    }

    private void StartRoomDetails()
    {
        State.ClearMembers();
        State.RoomDetailsSeen = true;
        State.RosterCompleteness = RosterCompleteness.Partial;
    }

    private void ApplyWorld(SessionPhase phase)
    {
        if (phase == SessionPhase.Lobby)
        {
            ClearRoomState();
            State.Phase = SessionPhase.Lobby;
            return;
        }

        State.Phase = phase;
        if (!State.IsRoomEntryPending) return;

        if (_configuredRole != LocalRole.Unknown && State.RoomDetailsSeen)
        {
            CompleteEntry();
            return;
        }

        if (phase == SessionPhase.Playing && State.MidgameEntrySeen)
        {
            State.LocalRole = LocalRole.Spectator;
            State.RoleEvidence = EvidenceKind.Inferred;
            CompleteEntry();
        }
        else if (phase == SessionPhase.Waiting && State.RoomDetailsSeen && !State.MidgameEntrySeen)
        {
            State.LocalRole = LocalRole.Joined;
            State.RoleEvidence = EvidenceKind.Inferred;
            CompleteEntry();
        }
    }

    private void CompleteEntry()
    {
        State.IsRoomEntryPending = false;
        State.RosterCompleteness = State.RoomDetailsSeen ? RosterCompleteness.Complete : RosterCompleteness.Partial;
    }

    private void ApplyMemberLeave(string roleId)
    {
        if (_localRoleId is not null && string.Equals(roleId, _localRoleId, StringComparison.Ordinal))
        {
            ClearRoomState();
            return;
        }

        State.RemoveMember(roleId);
    }

    private void ApplyRoomOperation(RoomOperationObserved operation)
    {
        if (operation.Operation != 1 || _localRoleId is null ||
            !string.Equals(operation.RoleId, _localRoleId, StringComparison.Ordinal) ||
            !State.IsRoleSwitchPending) return;

        State.LocalRole = State.LocalRole switch
        {
            LocalRole.Joined => LocalRole.Spectator,
            LocalRole.Spectator => LocalRole.Joined,
            _ => LocalRole.Unknown,
        };
        State.RoleEvidence = State.LocalRole == LocalRole.Unknown ? EvidenceKind.Unknown : EvidenceKind.Inferred;
        State.IsRoleSwitchPending = false;
    }

    private void ClearRoomState()
    {
        State.ClearMembers();
        State.Phase = SessionPhase.Unknown;
        State.LocalRole = LocalRole.Unknown;
        State.RoleEvidence = EvidenceKind.Unknown;
        State.RosterCompleteness = RosterCompleteness.Unknown;
        State.IsRoomEntryPending = false;
        State.IsRoleSwitchPending = false;
        State.RoomDetailsSeen = false;
        State.MidgameEntrySeen = false;
        State.Diagnostic = DiagnosticCode.None;
    }

    private void RefreshDiagnostic()
    {
        State.Diagnostic = State switch
        {
            { IsRoleSwitchPending: true } => DiagnosticCode.RoleSwitchAwaitingConfirmation,
            { IsRoomEntryPending: true } => DiagnosticCode.RoomEntryPending,
            { Phase: SessionPhase.Lobby } => DiagnosticCode.None,
            { RosterCompleteness: not RosterCompleteness.Complete } => DiagnosticCode.RosterIncomplete,
            { LocalRole: LocalRole.Unknown } => DiagnosticCode.LocalRoleUnknown,
            _ => DiagnosticCode.None,
        };
    }
}

public static class RoleId
{
    public static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && value.All(char.IsAsciiDigit);
}