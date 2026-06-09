using EngravingStation.Core.Services;
using Xunit;

namespace EngravingStation.Core.Tests;

public sealed class OrderCodeTests
{
    [Fact]
    public void Normalize_TrimsControlCharactersFullWidthAndUppercases()
    {
        var normalizer = new OrderCodeNormalizer();
        var normalized = normalizer.Normalize(" \u0002ｏｒｄ-１２ab\u0003 ");
        Assert.Equal("ORD-12AB", normalized);
    }

    [Theory]
    [InlineData("ORD-1234", true)]
    [InlineData("TRK000001", true)]
    [InlineData("BAD-1", false)]
    public void Validator_UsesConfigurableRegexRules(string code, bool expected)
    {
        var validator = new OrderCodeValidator(["^ORD-[0-9]{4}$", "^TRK[0-9]{6}$"]);
        Assert.Equal(expected, validator.IsValid(code));
    }
}
