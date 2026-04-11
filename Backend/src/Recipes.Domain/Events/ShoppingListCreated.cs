using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record ShoppingListCreated(
    ShoppingListId ShoppingListId,
    string Name) : IDomainEvent;