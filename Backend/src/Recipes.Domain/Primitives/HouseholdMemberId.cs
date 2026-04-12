namespace Recipes.Domain.Primitives;

public readonly record struct HouseholdMemberId(Guid Value)
{
    public static HouseholdMemberId New() => new(Guid.NewGuid());

    public static HouseholdMemberId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Household member id cannot be empty.", nameof(value))
            : new HouseholdMemberId(value);

    public override string ToString() => Value.ToString();
}