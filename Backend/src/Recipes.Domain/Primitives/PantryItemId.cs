namespace Recipes.Domain.Primitives;

public readonly record struct PantryItemId(Guid Value)
{
    public static PantryItemId New() => new(Guid.NewGuid());

    public static PantryItemId From(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("PantryItemId cannot be empty.", nameof(value));
        return new(value);
    }
}
