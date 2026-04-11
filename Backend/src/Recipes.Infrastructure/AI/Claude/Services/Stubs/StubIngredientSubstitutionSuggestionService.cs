using Recipes.Application.Recipes.SuggestIngredientSubstitutions;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubIngredientSubstitutionSuggestionService : IIngredientSubstitutionSuggestionService
{
    public Task<IngredientSubstitutionSuggestionDto> SuggestAsync(
        IngredientSubstitutionRequestDto request,
        CancellationToken cancellationToken)
    {
        var substitutes = request.IngredientName.Trim().ToLowerInvariant() switch
        {
            "egg" or "eggs" => new List<IngredientSubstituteDto>
            {
                new IngredientSubstituteDto(
                    "Flax egg",
                    "Works well as a binder in many baked recipes.",
                    "Use 1 tbsp ground flaxseed + 3 tbsp water per egg.",
                    false),
                new IngredientSubstituteDto(
                    "Mashed banana",
                    "Adds moisture and mild sweetness in baking.",
                    "Use about 1/4 banana per egg.",
                    false)
            },

            "butter" => new List<IngredientSubstituteDto>
            {
                new IngredientSubstituteDto(
                    "Olive oil",
                    "Works in sautéing and many savory dishes.",
                    "Use slightly less than the original butter amount.",
                    false),
                new IngredientSubstituteDto(
                    "Margarine",
                    "Closest direct replacement in many cooking scenarios.",
                    null,
                    true)
            },

            _ => new List<IngredientSubstituteDto>
            {
                new IngredientSubstituteDto(
                    "Olive oil",
                    "General-purpose substitution example from stub service.",
                    "Adjust to taste.",
                    false)
            }
        };

        var result = new IngredientSubstitutionSuggestionDto(
            request.IngredientName,
            substitutes,
            0.55,
            true,
            "Stub substitution suggestion. Replace with Claude-backed suggestions later.");

        return Task.FromResult(result);
    }
}