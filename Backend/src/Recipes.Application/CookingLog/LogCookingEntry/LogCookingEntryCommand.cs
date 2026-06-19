using ErrorOr;
using MediatR;

namespace Recipes.Application.CookingLog.LogCookingEntry;

public sealed record LogCookingEntryCommand(
    Guid RecipeId,
    DateOnly CookedOn,
    int Servings,
    string? Notes,
    IReadOnlyList<Guid>? PreparedByPersonIds = null) : IRequest<ErrorOr<CookingLogEntryDto>>;
