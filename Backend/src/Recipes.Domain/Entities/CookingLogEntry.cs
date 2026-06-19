namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class CookingLogEntry : Entity
{
    private readonly List<CookingLogPreparer> _preparedBy = new();

    public CookingLogEntryId Id { get; private set; } = CookingLogEntryId.New();
    public RecipeId RecipeId { get; private set; }
    public UserId UserId { get; private set; }
    public HouseholdId? HouseholdId { get; private set; }
    public DateOnly CookedOn { get; private set; }
    public int Servings { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<CookingLogPreparer> PreparedBy => _preparedBy.AsReadOnly();

    private CookingLogEntry() { }

    public CookingLogEntry(
        RecipeId recipeId,
        UserId userId,
        HouseholdId? householdId,
        DateOnly cookedOn,
        int servings,
        string? notes,
        DateTimeOffset now,
        IEnumerable<PersonId>? preparedBy = null)
    {
        RecipeId = recipeId;
        UserId = userId;
        HouseholdId = householdId;
        CookedOn = cookedOn;
        Servings = servings;
        Notes = notes?.Trim();
        CreatedAt = now;

        if (preparedBy is not null)
        {
            foreach (var personId in preparedBy)
                _preparedBy.Add(new CookingLogPreparer(personId));
        }
    }
}
