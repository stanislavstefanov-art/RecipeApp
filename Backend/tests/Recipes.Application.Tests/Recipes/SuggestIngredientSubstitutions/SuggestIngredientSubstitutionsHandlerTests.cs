using FluentAssertions;
using Recipes.Application.Recipes.SuggestIngredientSubstitutions;
using Recipes.Infrastructure.AI.Claude.Services.Stubs;

namespace Recipes.Application.Tests.Recipes.SuggestIngredientSubstitutions;

public sealed class SuggestIngredientSubstitutionsHandlerTests
{
    [Fact]
    public async Task Should_Return_Substitution_Suggestions()
    {
        var service = new StubIngredientSubstitutionSuggestionService();
        var handler = new SuggestIngredientSubstitutionsHandler(service);

        var result = await handler.Handle(
            new SuggestIngredientSubstitutionsCommand(
                "eggs",
                "Pancake batter",
                "egg-free"),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Substitutes.Should().NotBeEmpty();
        result.Value.OriginalIngredient.Should().Be("eggs");
    }
}