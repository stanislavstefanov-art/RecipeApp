namespace Recipes.Domain.Primitives;

public readonly record struct IngredientId(Guid Value)
{
    public static IngredientId New() => new(Guid.NewGuid());

    public static IngredientId From(Guid value) =>
        value == Guid.Empty ? throw new ArgumentException("Ingredient id cannot be empty.", nameof(value)) : new IngredientId(value);

    public override string ToString() => Value.ToString();
}

