using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public sealed record SuggestIngredientSubstitutionsCommand(
    string IngredientName,
    string? RecipeContext,
    string? DietaryGoal) : IRequest<ErrorOr<IngredientSubstitutionSuggestionDto>>;