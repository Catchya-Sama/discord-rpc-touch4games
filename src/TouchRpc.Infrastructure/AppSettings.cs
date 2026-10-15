using System.Text.Json;
using TouchRpc.Core;

namespace TouchRpc.Infrastructure;

public sealed record AppSettings(
    string? LogPath = null,
    string? LocalRoleId = null,
    LocalRole ConfiguredRole = LocalRole.Unknown,
    int AfkThresholdMinutes = 30,
    string? ManualNickname = null,
    string? ManualNicknameRoleId = null,
    string? DiscordApplicationId = null,
    string? DiscordLargeImageKey = null);

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public AppSettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TouchRpc",
            "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            await using var stream = File.OpenRead(_settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? new AppSettings();
            return Normalize(settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(settings);
        var directory = Path.GetDirectoryName(_settingsPath)
            ?? throw new InvalidOperationException("The settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = _settingsPath + ".tmp";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Move(temporaryPath, _settingsPath, overwrite: true);
    }

    private static AppSettings Normalize(AppSettings settings)
    {
        var roleId = settings.LocalRoleId;
        if (roleId is not null && !RoleId.IsValid(roleId)) roleId = null;
        var path = string.IsNullOrWhiteSpace(settings.LogPath) ? null : settings.LogPath.Trim();
        var thresholdMinutes = settings.AfkThresholdMinutes > 0 ? settings.AfkThresholdMinutes : 30;
        var manualNicknameRoleId = settings.ManualNicknameRoleId;
        if (manualNicknameRoleId is not null && !RoleId.IsValid(manualNicknameRoleId)) manualNicknameRoleId = null;
        var manualNickname = string.IsNullOrWhiteSpace(settings.ManualNickname)
            ? null
            : settings.ManualNickname.Trim();
        if (manualNicknameRoleId is null) manualNickname = null;
        var discordApplicationId = NormalizeDiscordApplicationId(settings.DiscordApplicationId);
        var discordLargeImageKey = string.IsNullOrWhiteSpace(settings.DiscordLargeImageKey)
            ? null
            : settings.DiscordLargeImageKey.Trim();
        if (discordLargeImageKey?.Length > 256) discordLargeImageKey = discordLargeImageKey[..256];
        return settings with
        {
            LogPath = path,
            LocalRoleId = roleId,
            AfkThresholdMinutes = thresholdMinutes,
            ManualNickname = manualNickname,
            ManualNicknameRoleId = manualNicknameRoleId,
            DiscordApplicationId = discordApplicationId,
            DiscordLargeImageKey = discordLargeImageKey,
        };
    }

    public static string? NormalizeDiscordApplicationId(string? applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId)) return null;
        var normalized = applicationId.Trim();
        return normalized.All(char.IsAsciiDigit) && ulong.TryParse(normalized, out _) ? normalized : null;
    }
}