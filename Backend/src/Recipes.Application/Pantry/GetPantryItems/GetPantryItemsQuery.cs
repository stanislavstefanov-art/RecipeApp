using ErrorOr;
using MediatR;

namespace Recipes.Application.Pantry.GetPantryItems;

public sealed record GetPantryItemsQuery : IRequest<ErrorOr<IReadOnlyList<PantryItemDto>>>;
