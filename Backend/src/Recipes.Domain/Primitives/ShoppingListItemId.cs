namespace Recipes.Domain.Primitives;

public readonly record struct ShoppingListItemId(Guid Value)
{
    public static ShoppingListItemId New() => new(Guid.NewGuid());

    public static ShoppingListItemId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Shopping list item id cannot be empty.", nameof(value))
            : new ShoppingListItemId(value);

    public override string ToString() => Value.ToString();
}