using System.Text.RegularExpressions;

namespace TouchRpc.Core;

public sealed partial class TouchLogParser
{
    public IReadOnlyList<LogEvent> ParseLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return [];

        var events = new List<LogEvent>();
        if (line.Contains("_OnCmd_RoomEnter", StringComparison.Ordinal)) events.Add(new RoomEntryRequested());
        if (line.Contains("_OnCmd_RoomDetailInfo_Re", StringComparison.Ordinal)) events.Add(new RoomDetailsStarted());

        AddMember(events, SnapshotMemberRegex(), line, MemberEvidence.Snapshot);
        AddMember(events, RoomMemberRegex(), line, MemberEvidence.RoomMember);
        AddMember(events, AddPlayerRegex(), line, MemberEvidence.AddPlayer);

        var leave = RoomLeaveRegex().Match(line);
        if (leave.Success) events.Add(new MemberLeft(leave.Groups[1].Value));
        if (line.Contains("_OnCmd_GetGameInfoRe", StringComparison.Ordinal)) events.Add(new MidgameEntryObserved());
        if (line.Contains("SwitchRoleType", StringComparison.Ordinal)) events.Add(new RoleSwitchRequested());

        var operation = OperationRegex().Match(line);
        if (operation.Success)
        {
            events.Add(new RoomOperationObserved(
                int.Parse(operation.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture),
                operation.Groups[2].Value));
        }

        if (line.Contains("the_current_world Type=WaitRoom", StringComparison.Ordinal))
            events.Add(new WorldObserved(SessionPhase.Waiting));
        else if (line.Contains("Type=RoleCreateRoom", StringComparison.Ordinal))
            events.Add(new WorldObserved(SessionPhase.Lobby));
        else if (line.Contains("Type=PlayRoom", StringComparison.Ordinal))
            events.Add(new WorldObserved(SessionPhase.Playing));

        if (line.Contains("StartPlayedTime", StringComparison.Ordinal) || line.Contains("Dancing", StringComparison.Ordinal))
            events.Add(new GameplayObserved());
        if (line.Contains("_OnCmd_MusicEnd", StringComparison.Ordinal) || line.Contains("Statistics", StringComparison.Ordinal))
            events.Add(new ResultsObserved());

        return events;
    }

    private static void AddMember(ICollection<LogEvent> events, Regex regex, string line, MemberEvidence evidence)
    {
        var match = regex.Match(line);
        if (match.Success) events.Add(new MemberObserved(match.Groups[1].Value, evidence));
    }

    [GeneratedRegex(@"RoomMemer:\s*(\d+)", RegexOptions.CultureInvariant)] private static partial Regex SnapshotMemberRegex();
    [GeneratedRegex(@"RoomMember\s+roleid\s*=\s*(\d+)", RegexOptions.CultureInvariant)] private static partial Regex RoomMemberRegex();
    [GeneratedRegex(@"AddPlayer\s+roleid\s*=\s*(\d+)", RegexOptions.CultureInvariant)] private static partial Regex AddPlayerRegex();
    [GeneratedRegex(@"_OnCmd_RoomLeave_Re\s+id:\s*(\d+)", RegexOptions.CultureInvariant)] private static partial Regex RoomLeaveRegex();
    [GeneratedRegex(@"oper\s*=\s*(\d+)\s+roleid\s*=\s*(\d+)", RegexOptions.CultureInvariant)] private static partial Regex OperationRegex();
}