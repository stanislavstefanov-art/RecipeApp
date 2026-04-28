using ErrorOr;
using MediatR;

namespace Recipes.Application.CookingLog.GetRecipeCookingHistory;

public sealed record GetRecipeCookingHistoryQuery(Guid RecipeId)
    : IRequest<ErrorOr<IReadOnlyList<CookingLogEntryDto>>>;
