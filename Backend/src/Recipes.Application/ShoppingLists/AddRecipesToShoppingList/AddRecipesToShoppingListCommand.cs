using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.AddRecipesToShoppingList;

public sealed record AddRecipesToShoppingListCommand(
    Guid ShoppingListId,
    IReadOnlyList<Guid> RecipeIds) : IRequest<ErrorOr<Success>>;