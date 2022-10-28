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

    private const string BlankLineAlsoNeededBeforeConstructor = @"
using Microsoft.Extensions.DependencyInjection;

namespace Phoneshop.WinForms
{
    public partial class PhoneOverview : Form
    {
        List<Phone> _phoneList = new();
        public PhoneOverview(IPhoneService service)
        {
        }
    }
}";

    [Fact]
    public void Should_report_absence_of_blank_line_before_constructor()
    {
        // arrange
        (FileTokenData fileTokenData, Report report) = Utilities.Setup(BlankLineAlsoNeededBeforeConstructor);

        //act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }
}