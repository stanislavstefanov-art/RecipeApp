namespace Recipes.Domain.Primitives;

public readonly record struct RecipeName
{
    public string Value { get; }

    public RecipeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Recipe name cannot be empty.", nameof(value));
        }

        var trimmed = value.Trim();

        if (trimmed.Length > 200)
        {
            throw new ArgumentException("Recipe name cannot be longer than 200 characters.", nameof(value));
        }

        Value = trimmed;
    }

    public override string ToString() => Value;
}

