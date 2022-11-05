namespace DTOsAndUtilities;

public static class WarningSettings
{
    public static int BasicMaxMethodLength { get; }
    public static int IdealMaxMethodLength { get; }

    public static int BasicMaxLineLength { get; }
    public static int IdealMaxLineLength { get; }

    public static bool UseLevels { get; }

    private const string DefaultSettingsText = @"
MaxMethodLength = 15 # Visser asks 15, 42 finds 25 enough
MaxLineLength = 120 # 100 is very showable on most windows without horizontal scrolling, 150 not";

    private const string SettingsFilename = "settings.txt";

    static WarningSettings()
    {
        if (!File.Exists(SettingsFilename)) File.WriteAllText(SettingsFilename, DefaultSettingsText);
        string[] lines = File.ReadAllLines(SettingsFilename);
        BasicMaxMethodLength = GetInt("BasicMaxMethodLength", lines, 25);
        IdealMaxMethodLength = GetInt("IdealMaxMethodLength", lines, 15);
        BasicMaxLineLength = GetInt("BasicMaxLineLength", lines, 140);
        IdealMaxLineLength = GetInt("IdealMaxLineLength", lines, 120);
        UseLevels = GetBool("UseLevels", lines, true);
    }

    private static int GetInt(string key, string[] lines, int defaultValue) =>
        Get(key, lines, defaultValue, int.Parse);

    private static bool GetBool(string key, string[] lines, bool defaultValue) =>
        Get(key, lines, defaultValue, bool.Parse);

    private static T Get<T>(string key, string[] lines, T defaultValue, Func<string, T> parse)
    {
        string? line = lines.FirstOrDefault(line => line.StartsWith(key));
        if (line == null) return defaultValue;
        return parse(line.Split()[2]);
    }
}