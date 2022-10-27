using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer.Tests;

public class ParameterNameTests
{
    private const string DetectWrongParameter = @"
var services = new ServiceCollection();

void DrawPhoneInformation(Phone selectedPhone, int PressedKey)
{
    ReturnKey();
}";

    [Fact]
    public void Should_detect_wrongly_cased_parameter()
    {
        // arrange
        (FileTokenData fileTokenData, Report report) = Utilities.Setup(DetectWrongParameter);

        //act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }
}