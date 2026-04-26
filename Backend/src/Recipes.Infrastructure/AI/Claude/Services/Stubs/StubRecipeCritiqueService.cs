using Recipes.Application.Recipes.CritiqueRecipe;
using Recipes.Application.Recipes.GetRecipe;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubRecipeCritiqueService : IRecipeCritiqueService
{
    public Task<RecipeCritiqueDto> CritiqueAsync(RecipeDto recipe, CancellationToken cancellationToken)
    {
        var dimensions = new List<CritiqueDimensionDto>
        {
            new(
                Dimension: "Clarity",
                Rating: 4,
                Rationale: "Steps are easy to follow and ingredient quantities are stated explicitly.",
                Suggestions: new[]
                {
                    "Add an estimated total time at the top.",
                    "Mention preferred pan size in the first step."
                }),
            new(
                Dimension: "Nutrition",
                Rating: 3,
                Rationale: "Balanced macros, but sodium relies on salt-to-taste rather than a measured amount.",
                Suggestions: new[]
                {
                    "Specify a starting salt quantity (e.g. 1/2 tsp).",
                    "Suggest a low-sodium variation."
                }),
            new(
                Dimension: "Approachability",
                Rating: 4,
                Rationale: "Suitable for a confident beginner; no specialised equipment required.",
                Suggestions: new[]
                {
                    "Call out which steps can be prepped ahead."
                })
        };

        var result = new RecipeCritiqueDto(
            Dimensions: dimensions,
            Summary: $"Stub critique for '{recipe.Name}'. Replace with Claude-backed critique in production.",
            Confidence: "0.6",
            NeedsReview: true,
            ProvenanceId: Guid.Empty);

        return Task.FromResult(result);
    }
}
