using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record ShoppingListItemRemoved(
    ShoppingListId ShoppingListId,
    ShoppingListItemId ShoppingListItemId,
    ProductId ProductId,
    string ProductName) : IDomainEvent;