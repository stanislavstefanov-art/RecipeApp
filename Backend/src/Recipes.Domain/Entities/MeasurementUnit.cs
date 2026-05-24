using Recipes.Domain.Primitives;

namespace Recipes.Domain.Entities;

public sealed class MeasurementUnit : Entity
{
    public MeasurementUnitId Id { get; private set; } = MeasurementUnitId.New();
    public string Name { get; private set; } = string.Empty;
    public string Abbreviation { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    private MeasurementUnit() { }

    public MeasurementUnit(string name, string abbreviation, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(abbreviation);
        Name = name.Trim();
        Abbreviation = abbreviation.Trim();
        SortOrder = sortOrder;
    }
}
