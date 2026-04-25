using FluentAssertions;
using Recipes.Application.Recipes.ImportRecipeFromUrl;

namespace Recipes.Application.Tests.Recipes.ImportRecipeFromUrl;

public sealed class ImportRecipeFromUrlValidatorTests
{
    private readonly ImportRecipeFromUrlValidator _validator = new();

    [Fact]
    public void Validate_EmptyUrl_Fails()
    {
        var result = _validator.Validate(new ImportRecipeFromUrlCommand(string.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RelativeUrl_Fails()
    {
        var result = _validator.Validate(new ImportRecipeFromUrlCommand("/recipes/pasta"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FtpUrl_Fails()
    {
        var result = _validator.Validate(new ImportRecipeFromUrlCommand("ftp://example.com/recipe"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_UrlExceedingMaxLength_Fails()
    {
        var url = "https://example.com/" + new string('a', 2048);

        var result = _validator.Validate(new ImportRecipeFromUrlCommand(url));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("https://www.seriouseats.com/carbonara")]
    [InlineData("http://example.com/recipe")]
    public void Validate_ValidAbsoluteHttpUrl_Passes(string url)
    {
        var result = _validator.Validate(new ImportRecipeFromUrlCommand(url));

        result.IsValid.Should().BeTrue();
    }
}
