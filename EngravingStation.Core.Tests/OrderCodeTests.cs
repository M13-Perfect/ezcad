using EngravingStation.Core.Services;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class OrderCodeTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   \t  ", "")]
    [InlineData(" \t ORD-1234 \t ", "ORD-1234")]
    [InlineData("\r\nord-1234\n", "ORD-1234")]
    [InlineData("trk000001", "TRK000001")]
    [InlineData("ｏｒｄ－１２Ａｂ", "ORD-12AB")]
    [InlineData(" \u0002ｏｒｄ-１２ab\u0003 ", "ORD-12AB")]
    public void Normalize_HandlesScannerInputVariants(string? input, string expected)
    {
        var normalizer = new OrderCodeNormalizer();

        var normalized = normalizer.Normalize(input);

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("ORD-1234")]
    [InlineData("ORD-12AB")]
    [InlineData("TRK000001")]
    [InlineData("TRK123456789")]
    public void DefaultValidator_AcceptsValidExamples(string code)
    {
        Assert.True(OrderCodeValidator.Default.IsValid(code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ORD-123")]
    [InlineData("ORD 1234")]
    [InlineData("BAD-1234")]
    [InlineData("TRKABCDEF")]
    public void DefaultValidator_RejectsInvalidFormats(string code)
    {
        Assert.False(OrderCodeValidator.Default.IsValid(code));
    }

    [Theory]
    [InlineData("ORD-1234", true)]
    [InlineData("TRK000001", true)]
    [InlineData("BAD-1", false)]
    public void Validator_UsesConfigurableRegexPatternRules(string code, bool expected)
    {
        var validator = new OrderCodeValidator(["^ORD-[0-9]{4}$", "^TRK[0-9]{6}$"]);

        Assert.Equal(expected, validator.IsValid(code));
    }

    [Fact]
    public void Validator_ReturnsMatchingConfiguredRule()
    {
        var validator = new OrderCodeValidator([
            new OrderCodeRule("Web order", "^WEB-[0-9]{5}$"),
            new OrderCodeRule("Retail order", "^RTL-[A-Z]{2}[0-9]{3}$")
        ]);

        var match = validator.Match("RTL-AB123");

        Assert.NotNull(match);
        Assert.Equal("Retail order", match.Name);
    }

    [Fact]
    public void Validator_RequiresAtLeastOneConfiguredRule()
    {
        var exception = Assert.Throws<ArgumentException>(() => new OrderCodeValidator(Array.Empty<OrderCodeRule>()));

        Assert.Contains("At least one", exception.Message, StringComparison.Ordinal);
    }
}
