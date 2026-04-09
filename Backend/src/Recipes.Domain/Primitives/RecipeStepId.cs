namespace Recipes.Domain.Primitives;

public readonly record struct RecipeStepId(Guid Value)
{
    public static RecipeStepId New() => new(Guid.NewGuid());

    public static RecipeStepId From(Guid value) =>
        value == Guid.Empty ? throw new ArgumentException("Recipe step id cannot be empty.", nameof(value)) : new RecipeStepId(value);

    public override string ToString() => Value.ToString();
}

