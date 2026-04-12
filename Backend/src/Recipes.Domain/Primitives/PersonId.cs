namespace Recipes.Domain.Primitives;

public readonly record struct PersonId(Guid Value)
{
    public static PersonId New() => new(Guid.NewGuid());

    public static PersonId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Person id cannot be empty.", nameof(value))
            : new PersonId(value);

    public override string ToString() => Value.ToString();
}