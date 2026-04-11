namespace Recipes.Domain.Primitives;

public readonly record struct RecipeIngredientId(Guid Value)
{
    public static RecipeIngredientId New() => new(Guid.NewGuid());

    public static RecipeIngredientId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Recipe ingredient id cannot be empty.", nameof(value))
            : new RecipeIngredientId(value);

    public override string ToString() => Value.ToString();
}