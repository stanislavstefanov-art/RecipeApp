namespace Recipes.Domain.Primitives;

public readonly record struct MealPlanPersonAssignmentId(Guid Value)
{
    public static MealPlanPersonAssignmentId New() => new(Guid.NewGuid());

    public static MealPlanPersonAssignmentId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Meal plan person assignment id cannot be empty.", nameof(value))
            : new MealPlanPersonAssignmentId(value);

    public override string ToString() => Value.ToString();
}