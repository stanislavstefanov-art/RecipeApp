using ErrorOr;
using MediatR;

namespace Recipes.Application.Pantry.RemovePantryItem;

public sealed record RemovePantryItemCommand(Guid Id) : IRequest<ErrorOr<Success>>;
