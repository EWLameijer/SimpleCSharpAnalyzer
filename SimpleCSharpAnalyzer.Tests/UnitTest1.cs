namespace SimpleCSharpAnalyzer.Tests;

public class UnitTest1
{
    private const string WrongNames = @"
    protected static int myFieldWrong3;
    protected static int _myFieldWrong4;
    private const int _mYConstWrong1 = 2;

    private const int _mYConstWrong2 = 4;

    public static int myFieldWrong1;
    public static int _myFieldWrong2;

    public static int myPropertyWrong { get; set; }
    public static int _myPropertyWrong { get; set; }

    public static int myPropertyWrong2 => 3;
    public static int _myPropertyWrong2 => 4;

    public void Test()
    {
        int MyWrongName;
        double _myOtherWrongName;
    }
";

    [Fact]
    public void Test1()
    {
    }
}