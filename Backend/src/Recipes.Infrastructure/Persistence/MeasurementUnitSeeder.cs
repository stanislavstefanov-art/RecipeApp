using Recipes.Domain.Entities;

namespace Recipes.Infrastructure.Persistence;

public sealed class MeasurementUnitSeeder
{
    private readonly RecipesDbContext _db;

    public MeasurementUnitSeeder(RecipesDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_db.MeasurementUnits.Any())
            return;

        _db.MeasurementUnits.AddRange(
            new MeasurementUnit("gram",        "g",     1),
            new MeasurementUnit("kilogram",    "kg",    2),
            new MeasurementUnit("milliliter",  "ml",    3),
            new MeasurementUnit("liter",       "l",     4),
            new MeasurementUnit("teaspoon",    "tsp",   5),
            new MeasurementUnit("tablespoon",  "tbsp",  6),
            new MeasurementUnit("piece",       "pcs",   7),
            new MeasurementUnit("pinch",       "pinch", 8),
            new MeasurementUnit("cup",         "cup",   9),
            new MeasurementUnit("clove",       "cloves",10),
            new MeasurementUnit("pack",        "pack",  11),
            new MeasurementUnit("milligram",   "mg",    12));

        await _db.SaveChangesAsync(cancellationToken);
    }
}
