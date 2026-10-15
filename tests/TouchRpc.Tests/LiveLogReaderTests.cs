using System.Text;
using TouchRpc.Infrastructure;

namespace TouchRpc.Tests;

[TestClass]
public sealed class LiveLogReaderTests
{
    [TestMethod]
    public async Task InitialReplayAndAppend_AreReadExactlyOnce()
    {
        using var temporary = new TemporaryLog("first\r\nsecond\r\n");
        var reader = new LiveLogReader(temporary.Path);

        var initial = await reader.ReadAvailableAsync();
        var waiting = await reader.ReadAvailableAsync();
        await temporary.AppendAsync("third\r\n");
        var appended = await reader.ReadAvailableAsync();

        CollectionAssert.AreEqual(new[] { "first", "second" }, initial.Lines.ToArray());
        Assert.IsTrue(initial.NewSession);
        Assert.IsEmpty(waiting.Lines);
        CollectionAssert.AreEqual(new[] { "third" }, appended.Lines.ToArray());
    }

    [TestMethod]
    public async Task PartialUtf8Line_IsBufferedUntilNewlineWithoutDuplication()
    {
        using var temporary = new TemporaryLog();
        var reader = new LiveLogReader(temporary.Path);
        _ = await reader.ReadAvailableAsync();

        var text = "RoomMember nama=玩家";
        var bytes = Encoding.UTF8.GetBytes(text + "\r\n");
        await temporary.AppendBytesAsync(bytes[..^2]);
        var partial = await reader.ReadAvailableAsync();
        await temporary.AppendBytesAsync(bytes[^2..]);
        var complete = await reader.ReadAvailableAsync();
        var waiting = await reader.ReadAvailableAsync();

        Assert.IsEmpty(partial.Lines);
        CollectionAssert.AreEqual(new[] { text }, complete.Lines.ToArray());
        Assert.IsEmpty(waiting.Lines);
    }

    [TestMethod]
    public async Task Truncation_StartsNewSessionAndDropsPartialState()
    {
        using var temporary = new TemporaryLog("old-partial");
        var reader = new LiveLogReader(temporary.Path);
        var initial = await reader.ReadAvailableAsync();
        Assert.IsEmpty(initial.Lines);

        await temporary.ReplaceContentsAsync("new\n");
        var reset = await reader.ReadAvailableAsync();

        Assert.IsTrue(reset.NewSession);
        CollectionAssert.AreEqual(new[] { "new" }, reset.Lines.ToArray());
    }

    [TestMethod]
    public async Task ReplacementLargerThanOldFile_StartsNewSession()
    {
        using var temporary = new TemporaryLog("old\n");
        var reader = new LiveLogReader(temporary.Path);
        _ = await reader.ReadAvailableAsync();

        await temporary.ReplaceFileAsync("replacement-is-larger\n");
        var reset = await reader.ReadAvailableAsync();

        Assert.IsTrue(reset.NewSession);
        CollectionAssert.AreEqual(new[] { "replacement-is-larger" }, reset.Lines.ToArray());
    }

    [TestMethod]
    public async Task SameLengthInPlaceRewrite_StartsNewSession()
    {
        using var temporary = new TemporaryLog("old-value\n");
        var reader = new LiveLogReader(temporary.Path);
        _ = await reader.ReadAvailableAsync();

        await temporary.ReplaceContentsAsync("new-value\n");
        var reset = await reader.ReadAvailableAsync();

        Assert.IsTrue(reset.NewSession);
        CollectionAssert.AreEqual(new[] { "new-value" }, reset.Lines.ToArray());
    }

    [TestMethod]
    public async Task MissingFile_ReturnsMissingHealthWithoutThrowing()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"touch-rpc-{Guid.NewGuid():N}.log");
        var reader = new LiveLogReader(path);

        var result = await reader.ReadAvailableAsync();

        Assert.AreEqual(LogSourceHealth.Missing, result.Health);
        Assert.IsEmpty(result.Lines);
    }

    [TestMethod]
    public async Task CanceledRead_ObservesCancellation()
    {
        using var temporary = new TemporaryLog(new string('x', 64 * 1024));
        var reader = new LiveLogReader(temporary.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await reader.ReadAvailableAsync(cancellation.Token));
    }

    private sealed class TemporaryLog : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"touch-rpc-{Guid.NewGuid():N}");

        public TemporaryLog(string contents = "")
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "output_log.txt");
            File.WriteAllText(Path, contents, new UTF8Encoding(false));
        }

        public string Path { get; }

        public Task AppendAsync(string text) => File.AppendAllTextAsync(Path, text, new UTF8Encoding(false));

        public async Task AppendBytesAsync(ReadOnlyMemory<byte> bytes)
        {
            await using var stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            await stream.WriteAsync(bytes);
        }

        public Task ReplaceContentsAsync(string contents) => File.WriteAllTextAsync(Path, contents, new UTF8Encoding(false));

        public async Task ReplaceFileAsync(string contents)
        {
            var replacement = System.IO.Path.Combine(_directory, "replacement.tmp");
            await File.WriteAllTextAsync(replacement, contents, new UTF8Encoding(false));
            File.Move(replacement, Path, overwrite: true);
        }

        public void Dispose()
        {
            try { Directory.Delete(_directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}