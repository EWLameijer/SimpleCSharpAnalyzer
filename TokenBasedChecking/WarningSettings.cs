namespace TokenBasedChecking;

public static class WarningSettings
{
    public static int MaxMethodLength { get; }
    public static int MaxLineLength { get; }

    static WarningSettings()
    {
        string[] lines = File.ReadAllLines("settings.txt");
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