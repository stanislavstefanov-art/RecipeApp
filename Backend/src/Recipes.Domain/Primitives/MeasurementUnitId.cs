namespace Recipes.Domain.Primitives;

public readonly record struct MeasurementUnitId(Guid Value)
{
    public static MeasurementUnitId New() => new(Guid.NewGuid());

    public static MeasurementUnitId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Measurement unit id cannot be empty.", nameof(value))
            : new MeasurementUnitId(value);

    public override string ToString() => Value.ToString();
}
