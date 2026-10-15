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
}