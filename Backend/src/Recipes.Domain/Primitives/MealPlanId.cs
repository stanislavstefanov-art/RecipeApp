namespace Recipes.Domain.Primitives;

public readonly record struct MealPlanId(Guid Value)
{
    public static MealPlanId New() => new(Guid.NewGuid());

    public static MealPlanId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Meal plan id cannot be empty.", nameof(value))
            : new MealPlanId(value);

    public override string ToString() => Value.ToString();
}