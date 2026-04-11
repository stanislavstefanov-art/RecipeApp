namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public sealed record IngredientSubstitutionSuggestionDto(
    string OriginalIngredient,
    IReadOnlyList<IngredientSubstituteDto> Substitutes,
    double Confidence,
    bool NeedsReview,
    string? Notes);

public sealed record IngredientSubstituteDto(
    string Name,
    string Reason,
    string? QuantityAdjustment,
    bool IsDirectReplacement);