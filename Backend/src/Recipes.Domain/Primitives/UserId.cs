namespace Recipes.Domain.Primitives;

public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());

    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("User id cannot be empty.", nameof(value))
            : new UserId(value);

    public override string ToString() => Value.ToString();
}
