namespace Recipes.Domain.Primitives;

public readonly record struct CookingLogEntryId(Guid Value)
{
    public static CookingLogEntryId New() => new(Guid.NewGuid());

    public static CookingLogEntryId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Cooking log entry id cannot be empty.", nameof(value))
            : new CookingLogEntryId(value);

    public override string ToString() => Value.ToString();
}
