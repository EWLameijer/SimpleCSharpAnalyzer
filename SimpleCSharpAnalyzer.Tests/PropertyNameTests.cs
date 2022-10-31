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
        (FileAsTokens fileTokenData, Report report) = Utilities.Setup(ErroneousPropertyName);

        // act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.GetWarnings());
    }

    private const string EnumPossiblySeenAsProperty = @"
namespace Phoneshop.Logging.Common;

public enum LogLevel : byte {
    Trace = 0,
}";

    [Fact]
    public void Should_not_report_enum_as_property()
    {
        // arrange
        (FileAsTokens fileTokenData, Report report) = Utilities.Setup(EnumPossiblySeenAsProperty);

        // act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Empty(report.GetWarnings());
    }
}