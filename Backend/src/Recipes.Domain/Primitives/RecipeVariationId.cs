namespace Recipes.Domain.Primitives;

public readonly record struct RecipeVariationId(Guid Value)
{
    public static RecipeVariationId New() => new(Guid.NewGuid());

    public static RecipeVariationId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Recipe variation id cannot be empty.", nameof(value))
            : new RecipeVariationId(value);

    public override string ToString() => Value.ToString();
}