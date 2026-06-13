using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.GetRecipe;

public sealed record GetRecipeQuery(Guid Id) : IRequest<ErrorOr<RecipeDto>>;

public sealed record RecipeDto(
    Guid Id,
    string Name,
    IReadOnlyList<IngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps,
    double? AverageStars,
    int RatingCount,
    IReadOnlyList<RecipeRatingDto> Ratings,
    RecipeRatingDto? MyRating,
    string? ImageUrl,
    int? DifficultyLevel,
    int RecipeType,
    int Origin,
    int MealsPerCook,
    IReadOnlyList<int> AppropriateForMealTypes);

public sealed record IngredientDto(Guid Id, string Name, decimal Quantity, string Unit);

public sealed record RecipeStepDto(Guid Id, int Order, string Instruction);
