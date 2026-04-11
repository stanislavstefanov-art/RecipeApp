using FluentAssertions;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Tests.Recipes.ImportRecipeFromText;

public sealed class ImportRecipeFromTextValidatorTests
{
    private readonly ImportRecipeFromTextValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Text_Is_Empty()
    {
        var command = new ImportRecipeFromTextCommand(string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_Text_Is_Too_Short()
    {
        var command = new ImportRecipeFromTextCommand("short");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_When_Text_Has_Enough_Content()
    {
        var command = new ImportRecipeFromTextCommand("2 eggs, 1 tomato, fry for 5 minutes");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}