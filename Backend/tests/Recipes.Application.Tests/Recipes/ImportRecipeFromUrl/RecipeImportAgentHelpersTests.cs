using FluentAssertions;
using Recipes.Infrastructure.AI.Claude.Agents;

namespace Recipes.Application.Tests.Recipes.ImportRecipeFromUrl;

public sealed class RecipeImportAgentHelpersTests
{
    // ── ParseQuantity ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("2",     2.0)]
    [InlineData("2.5",   2.5)]
    [InlineData("3/4",   0.75)]
    [InlineData("1/2",   0.5)]
    [InlineData("1 1/2", 1.5)]
    [InlineData("2 3/4", 2.75)]
    public void ParseQuantity_ValidInput_ReturnsParsedDecimal(string raw, double expected)
    {
        var result = RecipeImportAgent.ParseQuantity(raw);

        result.Should().BeApproximately((decimal)expected, 0.0001m);
    }

    [Theory]
    [InlineData("½",  0.5)]
    [InlineData("¼",  0.25)]
    [InlineData("¾",  0.75)]
    [InlineData("⅓",  0.3333)]
    [InlineData("⅛",  0.125)]
    public void ParseQuantity_UnicodeFractions_ReturnsParsedDecimal(string raw, double expected)
    {
        var result = RecipeImportAgent.ParseQuantity(raw);

        result.Should().BeApproximately((decimal)expected, 0.001m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("a lot")]
    public void ParseQuantity_Unparseable_ReturnsNull(string? raw)
    {
        var result = RecipeImportAgent.ParseQuantity(raw);

        result.Should().BeNull();
    }
}
