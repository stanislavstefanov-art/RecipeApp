using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SetAppropriateForMealTypes;

public sealed record SetAppropriateForMealTypesCommand(Guid RecipeId, IReadOnlyList<int> MealTypes)
    : IRequest<ErrorOr<Success>>;
