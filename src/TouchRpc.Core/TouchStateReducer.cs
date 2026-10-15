namespace TouchRpc.Core;

public sealed class TouchStateReducer
{
    private readonly string? _configuredLocalRoleId;
    private readonly LocalRole _configuredRole;
    private readonly IMonotonicClock _clock;
    private readonly TimeSpan _afkThreshold;
    private long? _afkStartedAt;
    private string? _observedLocalRoleId;
    private bool _historicalReplay;
    private bool _joinedGameplayObserved;

    public TouchStateReducer(
        string? localRoleId = null,
        LocalRole configuredRole = LocalRole.Unknown,
        IMonotonicClock? clock = null,
        TimeSpan? afkThreshold = null)
    {
        if (localRoleId is not null && !RoleId.IsValid(localRoleId))
            throw new ArgumentException("Local role ID must contain digits only.", nameof(localRoleId));

        _configuredLocalRoleId = localRoleId;
        _configuredRole = configuredRole;
        _clock = clock ?? SystemMonotonicClock.Instance;
        _afkThreshold = afkThreshold ?? AfkPolicy.DefaultThreshold;
        if (_afkThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(afkThreshold), "AFK threshold must be positive.");
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
            case GameplayObserved: ApplyGameplay(); break;
            case ResultsObserved: State.Phase = SessionPhase.Results; break;
            case RoleSwitchRequested:
                State.IsRoleSwitchPending = true;
                break;
            case RoomOperationObserved operation: ApplyRoomOperation(operation); break;
        }

        Tick();
        return State;
    }

    public TouchState Replay(IEnumerable<LogEvent> events)
    {
        foreach (var logEvent in events) Apply(logEvent);
        return State;
    }

    public void BeginHistoricalReplay() => _historicalReplay = true;

    public TouchState EndHistoricalReplay()
    {
        _historicalReplay = false;
        Tick();
        return State;
    }

    public TouchState Tick()
    {
        if (_afkStartedAt is long startedAt)
        {
            State.AfkElapsed = _clock.GetElapsedTime(startedAt, _clock.GetTimestamp());
            State.IsAfk = State.AfkElapsed >= _afkThreshold && State.LocalRole != LocalRole.Unknown;
        }
        else
        {
            State.AfkElapsed = TimeSpan.Zero;
            State.IsAfk = false;
        }

        RefreshDiagnostic();
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
        _joinedGameplayObserved = false;
        StopAfkInterval();
    }

    private void StartRoomDetails()
    {
        if (!State.IsRoomEntryPending) BeginRoomEntry();
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

        var previousPhase = State.Phase;
        State.Phase = phase;
        if (State.IsRoomEntryPending) StartAfkInterval();
        if (phase == SessionPhase.Waiting && previousPhase != SessionPhase.Waiting && _joinedGameplayObserved)
        {
            _joinedGameplayObserved = false;
            StartAfkInterval();
        }
        if (!State.IsRoomEntryPending)
        {
            StopAfkForKnownJoinedGameplay(phase);
            return;
        }

        if (_configuredRole != LocalRole.Unknown && State.RoomDetailsSeen)
        {
            CompleteEntry();
            StopAfkForKnownJoinedGameplay(phase);
            return;
        }

        if (phase == SessionPhase.Playing && State.RoomDetailsSeen)
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

        StopAfkForKnownJoinedGameplay(phase);
    }

    private void CompleteEntry()
    {
        State.IsRoomEntryPending = false;
        State.RosterCompleteness = State.RoomDetailsSeen ? RosterCompleteness.Complete : RosterCompleteness.Partial;
        StartAfkInterval();
    }

    private void ApplyMemberLeave(string roleId)
    {
        var localRoleId = _configuredLocalRoleId ?? _observedLocalRoleId;
        if (localRoleId is not null && string.Equals(roleId, localRoleId, StringComparison.Ordinal))
        {
            ClearRoomState();
            return;
        }

        State.RemoveMember(roleId);
    }

    private void ApplyRoomOperation(RoomOperationObserved operation)
    {
        if (operation.Operation != 1 || !State.IsRoleSwitchPending) return;

        var localRoleId = _configuredLocalRoleId ?? _observedLocalRoleId;
        if (localRoleId is null)
        {
            _observedLocalRoleId = operation.RoleId;
        }
        else if (!string.Equals(operation.RoleId, localRoleId, StringComparison.Ordinal))
        {
            return;
        }

        State.LocalRole = State.LocalRole switch
        {
            LocalRole.Joined => LocalRole.Spectator,
            LocalRole.Spectator => LocalRole.Joined,
            _ => LocalRole.Unknown,
        };
        State.RoleEvidence = State.LocalRole == LocalRole.Unknown ? EvidenceKind.Unknown : EvidenceKind.Inferred;
        State.IsRoleSwitchPending = false;
    }

    private void ApplyGameplay()
    {
        State.Phase = SessionPhase.Playing;
        if (State.LocalRole != LocalRole.Joined) return;

        _joinedGameplayObserved = true;
        StopAfkInterval();
    }

    private void StopAfkForKnownJoinedGameplay(SessionPhase phase)
    {
        if (phase != SessionPhase.Playing || State.LocalRole != LocalRole.Joined) return;
        _joinedGameplayObserved = true;
        StopAfkInterval();
    }

    private void StartAfkInterval()
    {
        if (_afkStartedAt is not null) return;
        _afkStartedAt = _clock.GetTimestamp();
        State.IsAfkTracking = true;
        State.IsAfk = false;
        State.AfkElapsed = TimeSpan.Zero;
        State.AfkTimerOrigin = _historicalReplay
            ? AfkTimerOrigin.MonitoringStarted
            : AfkTimerOrigin.RoomEntryObserved;
    }

    private void StopAfkInterval()
    {
        _afkStartedAt = null;
        State.IsAfkTracking = false;
        State.IsAfk = false;
        State.AfkElapsed = TimeSpan.Zero;
        State.AfkTimerOrigin = AfkTimerOrigin.None;
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
        _joinedGameplayObserved = false;
        StopAfkInterval();
    }

    private void RefreshDiagnostic()
    {
        State.Diagnostic = State switch
        {
            { IsRoleSwitchPending: true } => DiagnosticCode.RoleSwitchAwaitingConfirmation,
            { IsAfkTracking: true, LocalRole: LocalRole.Unknown } when State.AfkElapsed >= _afkThreshold => DiagnosticCode.AfkRoleUnknown,
            { IsRoomEntryPending: true } => DiagnosticCode.RoomEntryPending,
            { Phase: SessionPhase.Lobby } => DiagnosticCode.None,
            { RosterCompleteness: not RosterCompleteness.Complete } => DiagnosticCode.RosterIncomplete,
            { IsAfkTracking: true, AfkTimerOrigin: AfkTimerOrigin.MonitoringStarted } => DiagnosticCode.AfkHistoryUnavailable,
            { LocalRole: LocalRole.Unknown } => DiagnosticCode.LocalRoleUnknown,
            _ => DiagnosticCode.None,
        };
    }
}

public static class RoleId
{
    public static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) && value.All(char.IsAsciiDigit);
}