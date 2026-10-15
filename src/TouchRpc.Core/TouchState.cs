namespace TouchRpc.Core;

public sealed class TouchState
{
    private readonly HashSet<string> _members = new(StringComparer.Ordinal);

    public SessionPhase Phase { get; internal set; } = SessionPhase.Unknown;
    public LocalRole LocalRole { get; internal set; } = LocalRole.Unknown;
    public EvidenceKind RoleEvidence { get; internal set; } = EvidenceKind.Unknown;
    public RosterCompleteness RosterCompleteness { get; internal set; } = RosterCompleteness.Unknown;
    public bool IsRoomEntryPending { get; internal set; }
    public bool IsRoleSwitchPending { get; internal set; }
    public bool RoomDetailsSeen { get; internal set; }
    public bool MidgameEntrySeen { get; internal set; }
    public DiagnosticCode Diagnostic { get; internal set; } = DiagnosticCode.None;
    public int UniqueMemberCount => _members.Count;
    public IReadOnlyCollection<string> Members => _members;

    internal bool AddMember(string roleId) => _members.Add(roleId);
    internal bool RemoveMember(string roleId) => _members.Remove(roleId);
    internal void ClearMembers() => _members.Clear();
}