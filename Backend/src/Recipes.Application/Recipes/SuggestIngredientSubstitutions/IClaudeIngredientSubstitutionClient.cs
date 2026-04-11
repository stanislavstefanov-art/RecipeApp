namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public interface IClaudeIngredientSubstitutionClient
{
    Task<IngredientSubstitutionSuggestionDto> SuggestAsync(
        IngredientSubstitutionRequestDto request,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken);
}