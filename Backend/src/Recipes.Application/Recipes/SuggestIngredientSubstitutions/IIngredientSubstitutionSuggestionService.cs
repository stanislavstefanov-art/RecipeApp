namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public interface IIngredientSubstitutionSuggestionService
{
    Task<IngredientSubstitutionSuggestionDto> SuggestAsync(
        IngredientSubstitutionRequestDto request,
        CancellationToken cancellationToken);
}