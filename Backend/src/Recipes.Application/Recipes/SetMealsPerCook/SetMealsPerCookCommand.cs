using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SetMealsPerCook;

public sealed record SetMealsPerCookCommand(Guid RecipeId, int MealsPerCook) : IRequest<ErrorOr<Success>>;
