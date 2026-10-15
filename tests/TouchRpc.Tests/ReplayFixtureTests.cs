using TouchRpc.Core;

namespace TouchRpc.Tests;

[TestClass]
public sealed class ReplayFixtureTests
{
    [TestMethod]
    [DataRow("waiting-room.synthetic.log", LocalRole.Joined, SessionPhase.Waiting, 3, "3 Players Joined", "Waiting")]
    [DataRow("midgame-spectator.synthetic.log", LocalRole.Spectator, SessionPhase.Waiting, 2, "2 Players Joined", "Spectating")]
    [DataRow("role-switch.synthetic.log", LocalRole.Joined, SessionPhase.Waiting, 2, "2 Players Joined", "Waiting")]
    public void SyntheticReplay_ProducesExpectedFinalState(
        string fixture, LocalRole expectedRole, SessionPhase expectedPhase, int expectedCount,
        string expectedDetails, string expectedState)
    {
        var parser = new TouchLogParser();
        var reducer = new TouchStateReducer("10000000001");
        foreach (var line in File.ReadLines(GetFixturePath(fixture))) reducer.Replay(parser.ParseLine(line));

        var presence = PresenceFormatter.Format(reducer.State);
        Assert.AreEqual(expectedRole, reducer.State.LocalRole);
        Assert.AreEqual(expectedPhase, reducer.State.Phase);
        Assert.AreEqual(expectedCount, reducer.State.UniqueMemberCount);
        Assert.AreEqual(expectedDetails, presence.Details);
        Assert.AreEqual(expectedState, presence.State);
    }

    private static string GetFixturePath(string fixture) => Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture);
}