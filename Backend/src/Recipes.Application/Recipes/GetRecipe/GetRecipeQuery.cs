using MediatR;

namespace Recipes.Application.Recipes.GetRecipe;

public sealed record GetRecipeQuery(Guid Id) : IRequest<RecipeDto?>;

public sealed record RecipeDto(
    Guid Id,
    string Name,
    IReadOnlyList<IngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps);

public sealed record IngredientDto(string Name, decimal Quantity, string Unit);

public sealed record RecipeStepDto(int Order, string Instruction);

