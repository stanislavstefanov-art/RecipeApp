namespace Recipes.Domain.Primitives;

public readonly record struct ReceiptItemId(Guid Value)
{
    public static ReceiptItemId New() => new(Guid.NewGuid());

    public static ReceiptItemId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("ReceiptItem id cannot be empty.", nameof(value))
            : new ReceiptItemId(value);

    public override string ToString() => Value.ToString();
}
