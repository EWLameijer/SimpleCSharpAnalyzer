using DTOsAndUtilities;
using TokenBasedChecking;
using Tokenizing;

namespace SimpleCSharpAnalyzer.Tests;

public class UnitTest1
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
        Tokenizer tokenizer = new(InvalidMethodName.Split('\n'));
        IReadOnlyList<Token> tokens = tokenizer.Results();
        IReadOnlyList<Token> tokensWithoutAttributes = new TokenFilterer().Filter(tokens);
        FileTokenData fileTokenData = new("", tokensWithoutAttributes);
        LineCounter counter = new(tokens);
        Report report = counter.CreateReport();

        //act
        new IdentifierAndMethodLengthAnalyzer(fileTokenData, report).AddWarnings();

        // assert
        Assert.Single(report.Warnings);
    }
}