using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer.Tests;

public class BlankLineBetweenMethodsTests
{
    private const string LackOfBlankLineBetweenMethods = @"
namespace BrandServiceTests;

public class GetSingle : Base
{
    public void MethodA(int id)
    {
    }
    public void MethodB(int id)
    {
    }
}";

    [Fact]
    public void Should_report_lack_of_blank_lines_between_methods()
    {
        // arrange
        (FileTokenData fileTokenData, Report report) = Utilities.Setup(LackOfBlankLineBetweenMethods);

        //act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }
}