using FluentAssertions;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Tests.Recipes.ImportRecipeFromText;

public sealed class ImportedRecipeDtoValidatorTests
{
    private readonly ImportedRecipeDtoValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Ingredients_Are_Empty()
    {
        var dto = new ImportedRecipeDto(
            Title: "Imported recipe",
            Servings: 2,
            Ingredients: [],
            Steps: ["Cook it"],
            Notes: null,
            Confidence: 0.8,
            NeedsReview: false);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Steps_Are_Empty()
    {
        var dto = new ImportedRecipeDto(
            Title: "Imported recipe",
            Servings: 2,
            Ingredients: [new ImportedIngredientDto("Eggs", 2, "pcs", null)],
            Steps: [],
            Notes: null,
            Confidence: 0.8,
            NeedsReview: false);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_For_Valid_Dto()
    {
        var dto = new ImportedRecipeDto(
            Title: "Imported recipe",
            Servings: 2,
            Ingredients: [new ImportedIngredientDto("Eggs", 2, "pcs", null)],
            Steps: ["Cook it"],
            Notes: null,
            Confidence: 0.8,
            NeedsReview: false);

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}