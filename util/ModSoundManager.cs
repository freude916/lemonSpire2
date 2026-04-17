using Godot;

namespace lemonSpire2.util;

internal enum ModSound
{
    ReceiveMessage,
    AtMessage,
    SynergyNotice
}

internal static class ModSoundManager
{
    private static readonly IReadOnlyDictionary<ModSound, string> UserPaths = new Dictionary<ModSound, string>
    {
        [ModSound.ReceiveMessage] = "user://lemonSpire2/receive-message.mp3",
        [ModSound.AtMessage] = "user://lemonSpire2/at-message.mp3",
        [ModSound.SynergyNotice] = "user://lemonSpire2/synergy-notice.mp3"
    };

    private static readonly IReadOnlyDictionary<ModSound, string> DefaultRelativePaths =
        new Dictionary<ModSound, string>
        {
            [ModSound.ReceiveMessage] = Path.Combine("lemonSpire2", "receive-message.mp3"),
            [ModSound.AtMessage] = Path.Combine("lemonSpire2", "at-message.mp3"),
            [ModSound.SynergyNotice] = Path.Combine("lemonSpire2", "synergy-notice.mp3")
        };

    public static void Initialize()
    {
        var globalUserRoot = ProjectSettings.GlobalizePath("user://");
        MainFile.Log.Info($"user:// -> {globalUserRoot}");

        Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://lemonSpire2"));

        foreach (var sound in Enum.GetValues<ModSound>())
            EnsureDefaultSound(sound);
    }

    public static AudioStream? Load(ModSound sound)
    {
        var userPath = UserPaths[sound];
        var globalUserPath = ProjectSettings.GlobalizePath(userPath);
        if (!File.Exists(globalUserPath))
        {
            MainFile.Log.Warn($"Sound file missing, skipped: {userPath}");
            return null;
        }

        var stream = AudioStreamMP3.LoadFromFile(globalUserPath);
        if (stream == null)
            MainFile.Log.Warn($"Failed to load sound from {globalUserPath}");

        return stream;
    }

    private static void EnsureDefaultSound(ModSound sound)
    {
        var userPath = UserPaths[sound];
        var globalUserPath = ProjectSettings.GlobalizePath(userPath);
        if (File.Exists(globalUserPath))
            return;

        var modDirectory = ModPathResolver.ResolveModDirectory();
        var sourcePath = Path.Combine(modDirectory, DefaultRelativePaths[sound]);
        if (!File.Exists(sourcePath))
        {
            MainFile.Log.Warn($"Default sound file missing, skipped copy: {sourcePath}");
            return;
        }

        var targetDirectory = Path.GetDirectoryName(globalUserPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        File.Copy(sourcePath, globalUserPath, false);
        MainFile.Log.Info($"Copied default sound to {userPath}");
    }
}
