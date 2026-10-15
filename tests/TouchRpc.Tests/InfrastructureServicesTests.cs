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
    public async Task SettingsStore_RoundTripsEnabledPreferenceOutsideRepository()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        var store = new AppSettingsStore(path);
        var expected = new AppSettings(Enabled: false);

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public async Task SettingsStore_IgnoresLegacyManualConfiguration()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        await File.WriteAllTextAsync(path, """
            {
              "LogPath": "C:\\Games\\Touch\\output_log.txt",
              "LocalRoleId": "10000000001",
              "ConfiguredRole": 1,
              "DiscordApplicationId": "123456789012345678",
              "DiscordLargeImageKey": "logo",
              "Enabled": false
            }
            """);

        var actual = await new AppSettingsStore(path).LoadAsync();

        Assert.IsFalse(actual.Enabled);
    }

    [TestMethod]
    public async Task SettingsStore_DefaultsToEnabledWhenNoFileExists()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        var actual = await new AppSettingsStore(path).LoadAsync();

        Assert.IsTrue(actual.Enabled);
    }

    [TestMethod]
    public async Task SettingsStore_IgnoresLegacyManualNicknameFields()
    {
        using var temporary = new TemporaryDirectory();
        var path = Path.Combine(temporary.Path, "settings.json");
        var store = new AppSettingsStore(path);

        await File.WriteAllTextAsync(path, """
            {
              "LogPath": "C:\\Games\\Touch\\output_log.txt",
              "ManualNickname": "Legacy Name",
              "ManualNicknameRoleId": "10000000001"
            }
            """);
        var actual = await store.LoadAsync();
        await store.SaveAsync(actual);
        var migratedJson = await File.ReadAllTextAsync(path);

        Assert.IsTrue(actual.Enabled);
        Assert.DoesNotContain("ManualNickname", migratedJson, StringComparison.Ordinal);
        Assert.DoesNotContain("LogPath", migratedJson, StringComparison.Ordinal);
    }

    [TestMethod]
    public void DiscordApplicationId_NormalizationRemainsAvailableForTransportValidation()
    {
        Assert.AreEqual("123456789012345678", AppSettingsStore.NormalizeDiscordApplicationId(" 123456789012345678 "));
        Assert.IsNull(AppSettingsStore.NormalizeDiscordApplicationId("not-an-id"));
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