using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SetSeasonality;

public sealed record SetSeasonalityCommand(Guid RecipeId, int Seasonality)
    : IRequest<ErrorOr<Success>>;
