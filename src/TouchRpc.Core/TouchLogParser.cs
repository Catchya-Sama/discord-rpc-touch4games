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

        var localIdentity = LocalIdentityRegex().Match(line);
        if (localIdentity.Success) events.Add(new LocalIdentityObserved(localIdentity.Groups[1].Value));

        var world = CurrentWorldRegex().Match(line);
        if (world.Success)
        {
            var phase = world.Groups[1].Value switch
            {
                "WaitRoom" => SessionPhase.Waiting,
                "RoleCreateRoom" => SessionPhase.Lobby,
                "PlayRoom" => SessionPhase.Playing,
                _ => SessionPhase.Unknown,
            };
            if (phase != SessionPhase.Unknown) events.Add(new WorldObserved(phase));
        }

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
    [GeneratedRegex(@"the_current_world\s+Type=([A-Za-z]+)", RegexOptions.CultureInvariant)] private static partial Regex CurrentWorldRegex();
    [GeneratedRegex(@"formData:[^\r\n]*?roleid\s*=\s*(\d+)[^\r\n]*?isuserself\s*=\s*True\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)] private static partial Regex LocalIdentityRegex();
}