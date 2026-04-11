namespace Recipes.Domain.Primitives;

public readonly record struct ShoppingListId(Guid Value)
{
    public static ShoppingListId New() => new(Guid.NewGuid());

    public static ShoppingListId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Shopping list id cannot be empty.", nameof(value))
            : new ShoppingListId(value);

    public override string ToString() => Value.ToString();
}