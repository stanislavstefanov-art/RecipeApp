using ErrorOr;
using MediatR;

namespace Recipes.Application.Pantry.AddPantryItem;

public sealed record AddPantryItemCommand(string IngredientName, string? Notes) : IRequest<ErrorOr<PantryItemDto>>;
