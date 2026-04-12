namespace Recipes.Domain.Primitives;

public readonly record struct ExpenseId(Guid Value)
{
    public static ExpenseId New() => new(Guid.NewGuid());

    public static ExpenseId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Expense id cannot be empty.", nameof(value))
            : new ExpenseId(value);

    public override string ToString() => Value.ToString();
}