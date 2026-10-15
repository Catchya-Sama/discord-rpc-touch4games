using System.Text.Json;
using TouchRpc.Core;
using TouchRpc.Infrastructure;

namespace TouchRpc.Tests;

[TestClass]
public sealed class InfrastructureServicesTests
{
    [TestMethod]
    public void BuildLogPath_UsesExecutableDirectoryInsteadOfFixedInstallPath()
    {
        var executable = Path.Combine("X:\\Games", "CustomTouch", "TouchClient.exe");

        var logPath = TouchProcessDetector.BuildLogPath(executable);

        Assert.AreEqual(Path.Combine("X:\\Games", "CustomTouch", "TouchClient_Data", "output_log.txt"), logPath);
    }

    [TestMethod]
    public async Task SettingsStore_RoundTripsLocalSettingsOutsideRepository()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        var store = new AppSettingsStore(path);
        var expected = new AppSettings(
            "C:\\Games\\Touch\\output_log.txt",
            "10000000001",
            LocalRole.Spectator,
            45,
            "Synthetic Nickname",
            "10000000001");

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task SettingsStore_DropsInvalidRoleId()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new AppSettings(null, "launcher-user", LocalRole.Joined)));

        var actual = await new AppSettingsStore(path).LoadAsync();

        Assert.IsNull(actual.LocalRoleId);
        Assert.AreEqual(LocalRole.Joined, actual.ConfiguredRole);
        Assert.AreEqual(30, actual.AfkThresholdMinutes);
    }

    [TestMethod]
    public async Task SettingsStore_ReplacesNonPositiveAfkThresholdWithDefault()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new AppSettings(AfkThresholdMinutes: 0)));

        var actual = await new AppSettingsStore(path).LoadAsync();

        Assert.AreEqual(30, actual.AfkThresholdMinutes);
    }

    [TestMethod]
    public async Task SettingsStore_DoesNotPersistUnboundManualNickname()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        var store = new AppSettingsStore(path);

        await store.SaveAsync(new AppSettings(ManualNickname: "Unbound Name"));
        var actual = await store.LoadAsync();

        Assert.IsNull(actual.ManualNickname);
        Assert.IsNull(actual.ManualNicknameRoleId);
    }

    [TestMethod]
    public void LogPathValidator_RejectsBinaryContentButAllowsEmptyWaitingLog()
    {
        using var temporary = new TemporaryDirectory();
        var emptyPath = Path.Combine(temporary.Path, "empty.txt");
        var binaryPath = Path.Combine(temporary.Path, "binary.txt");
        File.WriteAllText(emptyPath, string.Empty);
        File.WriteAllBytes(binaryPath, [1, 0, 2, 3]);

        var empty = LogPathValidator.Validate(emptyPath);
        var binary = LogPathValidator.Validate(binaryPath);

        Assert.IsTrue(empty.IsValid);
        Assert.AreEqual(LogSourceHealth.ValidWaiting, empty.Health);
        Assert.IsFalse(binary.IsValid);
        Assert.AreEqual(LogSourceHealth.Unreadable, binary.Health);
    }

    [TestMethod]
    public void ClearForGameClosed_ResetsPresenceState()
    {
        using var temporary = new TemporaryDirectory();
        var logPath = Path.Combine(temporary.Path, "output_log.txt");
        File.WriteAllText(logPath, string.Empty);
        var session = new MonitoringSession(logPath, configuredRole: LocalRole.Joined);

        var snapshot = session.ClearForGameClosed();

        Assert.AreEqual(LogSourceHealth.GameClosed, snapshot.SourceHealth);
        Assert.AreEqual(SessionPhase.Unknown, snapshot.Phase);
        Assert.AreEqual(LocalRole.Unknown, snapshot.LocalRole);
        Assert.AreEqual("No active game", snapshot.Presence.Details);
        Assert.AreEqual(string.Empty, snapshot.Presence.State);
        Assert.IsNull(snapshot.MemberCount);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"touch-rpc-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { Directory.Delete(Path, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}