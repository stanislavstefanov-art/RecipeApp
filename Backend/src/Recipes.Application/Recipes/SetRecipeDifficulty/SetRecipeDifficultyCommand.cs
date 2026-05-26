using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SetRecipeDifficulty;

public sealed record SetRecipeDifficultyCommand(Guid RecipeId, int? DifficultyLevel) : IRequest<ErrorOr<Success>>;
