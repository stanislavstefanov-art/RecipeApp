namespace Recipes.Domain.Primitives;

public readonly record struct RecipeRatingId(Guid Value)
{
    public static RecipeRatingId New() => new(Guid.NewGuid());

    public static RecipeRatingId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Recipe rating id cannot be empty.", nameof(value))
            : new RecipeRatingId(value);

    public override string ToString() => Value.ToString();
}
