using DTOsAndUtilities;
using TokenBasedChecking;

namespace SimpleCSharpAnalyzer.Tests;

public class MethodNameTests
{
    private const string InvalidMethodName = @"
namespace BrandServiceTests;

public class GetSingle : Base
{
    public void Should_ThrowArgumentException_When_IdIsInvalid(int id)
    {
        void a() => brands.Get(id);
    }
}";

    [Fact]
    public void Invalid_method_names_should_be_detected()
    {
        // arrange
        (FileAsTokens fileTokenData, Report report) = Utilities.Setup(InvalidMethodName);

        // act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }

    private const string DoublyReportedMethodName = @"
namespace PhoneShop.WinForms
{
    public partial class PhoneOverview : Form
    {
        private void btn_add_Click(object sender, EventArgs e)
        {
            AddPhoneForm myNewAddPhoneForm = new AddPhoneForm(_myPhoneService, this);
            myNewAddPhoneForm.Show();
        }
    }
}";

    [Fact]
    public void Should_not_double_report_invalid_method_names()
    {
        // arrange
        (FileAsTokens fileTokenData, Report report) = Utilities.Setup(DoublyReportedMethodName);

        // act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }
}