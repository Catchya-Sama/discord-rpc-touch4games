using System.Text.Json;
namespace TouchRpc.Infrastructure;

public sealed record AppSettings(bool Enabled = true);

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
        return settings;
    }

    public static string? NormalizeDiscordApplicationId(string? applicationId)
    {
        if (string.IsNullOrWhiteSpace(applicationId)) return null;
        var normalized = applicationId.Trim();
        return normalized.All(char.IsAsciiDigit) && ulong.TryParse(normalized, out _) ? normalized : null;
    }
}