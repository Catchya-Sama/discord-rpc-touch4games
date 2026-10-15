using TouchRpc.Core;

namespace TouchRpc.Tests;

[TestClass]
public sealed class ParserTests
{
    private readonly TouchLogParser _parser = new();

    [TestMethod]
    public void ParseLine_RecognizesDocumentedMemberPatterns()
    {
        var events = new[]
        {
            "RoomMemer: 10000000001",
            "RoomMember roleid = 10000000002",
            "AddPlayer roleid = 10000000003",
        }.SelectMany(_parser.ParseLine).OfType<MemberObserved>().ToArray();

        CollectionAssert.AreEqual(
            new[] { "10000000001", "10000000002", "10000000003" },
            events.Select(item => item.RoleId).ToArray());
    }

    [TestMethod]
    public void ParseLine_IgnoresSlotChangesWithoutRoleId() =>
        Assert.IsEmpty(_parser.ParseLine("oper = 12 AddPlayer DelPlayer"));

    [TestMethod]
    public void ParseLine_DoesNotExposeUnknownContentAsEvent() =>
        Assert.IsEmpty(_parser.ParseLine("chat sensitive-content unrelated text"));

    [TestMethod]
    public void ParseLine_UsesCurrentWorldAndIgnoresLastWorld()
    {
        var waiting = _parser.ParseLine(
            "the_current_world Type=WaitRoom -------the_last_world Type=RoleCreateRoom");
        var lobby = _parser.ParseLine(
            "the_current_world Type=RoleCreateRoom -------the_last_world Type=PlayRoom");

        Assert.AreEqual(SessionPhase.Waiting, waiting.OfType<WorldObserved>().Single().Phase);
        Assert.AreEqual(SessionPhase.Lobby, lobby.OfType<WorldObserved>().Single().Phase);
    }
}