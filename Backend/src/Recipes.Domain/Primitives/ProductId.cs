namespace Recipes.Domain.Primitives;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());

    public static ProductId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Product id cannot be empty.", nameof(value))
            : new ProductId(value);

    public override string ToString() => Value.ToString();
}