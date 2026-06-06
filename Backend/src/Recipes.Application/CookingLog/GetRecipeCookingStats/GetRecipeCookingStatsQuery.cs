using ErrorOr;
using MediatR;

namespace Recipes.Application.CookingLog.GetRecipeCookingStats;

public sealed record GetRecipeCookingStatsQuery : IRequest<ErrorOr<IReadOnlyList<RecipeCookingStatDto>>>;

public sealed record RecipeCookingStatDto(Guid RecipeId, string RecipeName, int TimesCooked, DateOnly LastCookedOn);
