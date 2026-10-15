using System.Text;
using TouchRpc.Core;
using TouchRpc.Infrastructure;

namespace TouchRpc.Tests;

[TestClass]
public sealed class MonitoringSessionTests
{
    [TestMethod]
    public async Task InitialReplay_PublishesOnlyFinalConsistentSnapshot()
    {
        using var temporary = new TemporaryLog(string.Join("\n",
        [
            "_OnCmd_RoomEnter_Re",
            "_OnCmd_RoomDetailInfo_Re",
            "RoomMemer: 10000000001",
            "RoomMemer: 10000000002",
            "the_current_world Type=WaitRoom",
            string.Empty,
        ]));
        var session = new MonitoringSession(temporary.Path, "10000000001");

        var snapshot = await session.PollAsync();

        Assert.IsTrue(snapshot.NewSession);
        Assert.AreEqual(5, snapshot.ProcessedLines);
        Assert.AreEqual("2 Players Joined", snapshot.Presence.Details);
        Assert.AreEqual("Waiting", snapshot.Presence.State);
    }

    [TestMethod]
    public async Task TruncateAndNewRoom_DoesNotKeepOldRoster()
    {
        using var temporary = new TemporaryLog(string.Join("\n",
        [
            "_OnCmd_RoomEnter_Re", "_OnCmd_RoomDetailInfo_Re",
            "RoomMemer: 10000000001", "RoomMemer: 10000000002",
            "the_current_world Type=WaitRoom", string.Empty,
        ]));
        var session = new MonitoringSession(temporary.Path, "10000000001");
        _ = await session.PollAsync();

        await File.WriteAllTextAsync(temporary.Path, string.Join("\n",
        [
            "_OnCmd_RoomEnter_Re", "_OnCmd_RoomDetailInfo_Re",
            "RoomMemer: 10000000001", "the_current_world Type=WaitRoom", string.Empty,
        ]), new UTF8Encoding(false));
        var snapshot = await session.PollAsync();

        Assert.IsTrue(snapshot.NewSession);
        Assert.AreEqual(1, snapshot.MemberCount);
        Assert.AreEqual("1 Player Joined", snapshot.Presence.Details);
    }

    private sealed class TemporaryLog : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"touch-rpc-{Guid.NewGuid():N}");

        public TemporaryLog(string contents)
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "output_log.txt");
            File.WriteAllText(Path, contents, new UTF8Encoding(false));
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(_directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}