namespace Recipes.Domain.Primitives;

public readonly record struct RecipeId(Guid Value)
{
    public static RecipeId New() => new(Guid.NewGuid());

    public static RecipeId From(Guid value) =>
        value == Guid.Empty ? throw new ArgumentException("Recipe id cannot be empty.", nameof(value)) : new RecipeId(value);

    public override string ToString() => Value.ToString();
}

