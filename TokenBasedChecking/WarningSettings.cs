namespace TokenBasedChecking;

public static class WarningSettings
{
    public static int MaxMethodLength { get; }
    public static int MaxLineLength { get; }

    public static bool UseLevels { get; }

    private const string DefaultSettingsText = @"
MaxMethodLength = 15 # Visser asks 15, 42 finds 25 enough
MaxLineLength = 120 # 100 is very showable on most windows without horizontal scrolling, 150 not";

    private const string SettingsFilename = "settings.txt";

    static WarningSettings()
    {
        if (!File.Exists(SettingsFilename)) File.WriteAllText(SettingsFilename, DefaultSettingsText);
        string[] lines = File.ReadAllLines(SettingsFilename);
        MaxMethodLength = GetInt("MaxMethodLength", lines, 15);
        MaxLineLength = GetInt("MaxLineLength", lines, 120);
        UseLevels = GetBool("UseLevels", lines, true);
    }

    private static int GetInt(string key, string[] lines, int defaultValue)
    {
        string? line = lines.FirstOrDefault(line => line.StartsWith(key));
        if (line == null) return defaultValue;
        return int.Parse(line.Split()[2]);
    }

    private static bool GetBool(string key, string[] lines, bool defaultValue)
    {
        string? line = lines.FirstOrDefault(line => line.StartsWith(key));
        if (line == null) return defaultValue;
        return bool.Parse(line.Split()[2]);
    }
}