namespace TouchRpc.Core;

public abstract record LogEvent;
public sealed record RoomEntryRequested : LogEvent;
public sealed record RoomDetailsStarted : LogEvent;
public sealed record MemberObserved(string RoleId, MemberEvidence Evidence) : LogEvent;
public sealed record MemberLeft(string RoleId) : LogEvent;
public sealed record WorldObserved(SessionPhase Phase) : LogEvent;
public sealed record MidgameEntryObserved : LogEvent;
public sealed record GameplayObserved : LogEvent;
public sealed record ResultsObserved : LogEvent;
public sealed record RoleSwitchRequested : LogEvent;
public sealed record RoomOperationObserved(int Operation, string RoleId) : LogEvent;
public sealed record LocalIdentityObserved(string RoleId) : LogEvent;
public sealed record LocalNicknameObserved(string RoleId, string Nickname) : LogEvent;