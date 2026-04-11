using ErrorOr;
using MediatR;

namespace Recipes.Application.ShoppingLists.AddRecipeToShoppingList;

public sealed record AddRecipeToShoppingListCommand(
    Guid ShoppingListId,
    Guid RecipeId) : IRequest<ErrorOr<Success>>;