using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record ShoppingListItemAdded(
    ShoppingListId ShoppingListId,
    ShoppingListItemId ShoppingListItemId,
    ProductId ProductId,
    string ProductName,
    decimal Quantity,
    string Unit) : IDomainEvent;