using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer.Tests;

public class PropertyNameTests
{
    private const string ErroneousPropertyName = @"
namespace Phoneshop.WinForms;
internal static class Services
{
    internal static IServiceProvider serviceProvider { get; set; }
}
";

    [Fact]
    public void Should_report_erroneous_property_names()
    {
        // arrange
        (FileTokenData fileTokenData, Report report) = Utilities.Setup(ErroneousPropertyName);

        //act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }
}