using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed record CreateRecipeCommand(string Name, Guid HouseholdId, int RecipeType = 1, bool IsImported = false, int? DifficultyLevel = null) : IRequest<ErrorOr<CreateRecipeResponse>>;

public sealed record CreateRecipeResponse(Guid Id);
