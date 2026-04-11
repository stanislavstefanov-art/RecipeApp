namespace Recipes.Domain.Primitives;

public readonly record struct MealPlanEntryId(Guid Value)
{
    public static MealPlanEntryId New() => new(Guid.NewGuid());

    public static MealPlanEntryId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Meal plan entry id cannot be empty.", nameof(value))
            : new MealPlanEntryId(value);

    public override string ToString() => Value.ToString();
}