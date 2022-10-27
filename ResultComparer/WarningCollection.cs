using ResultsComparer;

namespace ResultComparer;

internal class WarningCollection
{
    private readonly Dictionary<string, List<WarningData>> _merged = new();

    public WarningCollection(string filePath)
    {
        Console.WriteLine($"Analyzing {filePath}");
        string[] lines = File.ReadAllLines(filePath);

        IEnumerable<WarningData> errorData = GetRawWarningData(lines);
        Console.WriteLine($"Before merging: {errorData.Count()}");
        Merge(errorData);
    }

    public void Show()
    {
        int count = 0;
        foreach (KeyValuePair<string, List<WarningData>> g in _merged)
        {
            Console.WriteLine(g.Key);
            IOrderedEnumerable<WarningData> ordered = g.Value.OrderBy(v => v.ToString());
            foreach (WarningData? currentWarning in ordered)
            {
                Console.WriteLine(currentWarning);
                count++;
            }
        }
        Console.WriteLine($"Total: {count}");
    }

    private static IEnumerable<WarningData> GetRawWarningData(string[] lines)
    {
        List<string> reportLines = lines.SkipWhile(line => !line.StartsWith("***TOTAL")).
                    SkipWhile(line => !line.StartsWith("1.")).
                    TakeWhile(line => line == "" || StartsWithNumberPoint(line)).Where(line => line != "").ToList();
        return reportLines.Select(line => new WarningData(line));
    }

    private static bool StartsWithNumberPoint(string line)
    {
        if (line.Length < 2) return false;
        List<char> number = line.TakeWhile(c => char.IsDigit(c)).ToList();
        if (number.Count == 0) return false;
        return line.Length > number.Count && line[number.Count] == '.';
    }

    public void Compare(WarningCollection other)
    {
        Compare(this, other);

        Compare(other, this);
    }

    private static void Compare(WarningCollection first, WarningCollection second)
    {
        foreach (KeyValuePair<string, List<WarningData>> entry in first._merged)
        {
            string key = entry.Key;
            List<WarningData> otherList = second._merged.ContainsKey(key) ?
                second._merged[key] : new List<WarningData>();
            foreach (WarningData item in entry.Value)
            {
                string warningText = item.WarningText;
                int idsCount = item.IdsCount;
                if (!otherList.Any(i => i.WarningText == warningText && i.IdsCount >= idsCount))
                {
                    Console.WriteLine($"Missing {item}");
                }
            }
        }
    }

    private void Merge(IEnumerable<WarningData> errorData)
    {
        IOrderedEnumerable<IGrouping<string, WarningData>> groups =
            errorData.ToLookup(e => e.WarningType).OrderBy(w => w.Key);

        foreach (IGrouping<string, WarningData>? g in groups)
        {
            string currentCategory = g.Key;
            Console.WriteLine(currentCategory);
            _merged[g.Key] = new();
            IOrderedEnumerable<WarningData> ordered = g.OrderBy(v => v.ToString());
            WarningData? last = null;
            foreach (WarningData? currentWarning in ordered)
            {
                if (last == null)
                {
                    last = currentWarning;
                }
                else
                {
                    last = LastFromMerge(currentCategory, last, currentWarning);
                }
                Console.WriteLine(currentWarning);
            }
            if (last != null) _merged[g.Key].Add(last);
        }
    }

    private WarningData LastFromMerge(string currentCategory, WarningData last, WarningData currentWarning)
    {
        if (currentWarning.WarningText != last.WarningText)
        {
            _merged[currentCategory].Add(last);
            last = currentWarning;
        }
        else
        {
            last.Merge(currentWarning);
        }

        return last;
    }
}