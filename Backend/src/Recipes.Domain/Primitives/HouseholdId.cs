namespace Recipes.Domain.Primitives;

public readonly record struct HouseholdId(Guid Value)
{
    public static HouseholdId New() => new(Guid.NewGuid());

    public static HouseholdId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Household id cannot be empty.", nameof(value))
            : new HouseholdId(value);

    public override string ToString() => Value.ToString();
}