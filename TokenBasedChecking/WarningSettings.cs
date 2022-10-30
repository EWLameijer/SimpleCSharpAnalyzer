namespace TokenBasedChecking;

public static class WarningSettings
{
    public static int MaxMethodLength { get; }
    public static int MaxLineLength { get; }

    private const string DefaultSettingsText = @"
MaxMethodLength = 15 # Visser asks 15, 42 finds 25 enough
MaxLineLength = 120 # 100 is very showable on most windows without horizontal scrolling, 150 not";

    private const string SettingsFilename = "settings.txt";

    static WarningSettings()
    {
        if (!File.Exists(SettingsFilename)) File.WriteAllText(SettingsFilename, DefaultSettingsText);
        string[] lines = File.ReadAllLines(SettingsFilename);
        MaxMethodLength = Get("MaxMethodLength", lines, 15);
        MaxLineLength = Get("MaxLineLength", lines, 120);
    }

    private static int Get(string key, string[] lines, int defaultValue)
    {
        string? line = lines.FirstOrDefault(line => line.StartsWith(key));
        if (line == null) return defaultValue;
        return int.Parse(line.Split()[2]);
    }
}