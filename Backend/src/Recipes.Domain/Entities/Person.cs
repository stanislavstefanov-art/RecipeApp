namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

public sealed class Person : Entity
{
    private readonly List<DietaryPreference> _dietaryPreferences = [];
    private readonly List<HealthConcern> _healthConcerns = [];

    public PersonId Id { get; private set; } = PersonId.New();
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<DietaryPreference> DietaryPreferences => _dietaryPreferences.AsReadOnly();
    public IReadOnlyCollection<HealthConcern> HealthConcerns => _healthConcerns.AsReadOnly();
    public string? Notes { get; private set; }

    private Person() { }

    public Person(
        string name,
        IEnumerable<DietaryPreference>? dietaryPreferences = null,
        IEnumerable<HealthConcern>? healthConcerns = null,
        string? notes = null)
    {
        Rename(name);
        SetDietaryPreferences(dietaryPreferences ?? []);
        SetHealthConcerns(healthConcerns ?? []);
        UpdateNotes(notes);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Person name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public void SetDietaryPreferences(IEnumerable<DietaryPreference> preferences)
    {
        _dietaryPreferences.Clear();
        _dietaryPreferences.AddRange(
            preferences
                .Where(x => x != DietaryPreference.None)
                .Distinct());
    }

    public void SetHealthConcerns(IEnumerable<HealthConcern> concerns)
    {
        _healthConcerns.Clear();
        _healthConcerns.AddRange(
            concerns
                .Where(x => x != HealthConcern.None)
                .Distinct());
    }

    public void UpdateNotes(string? notes)
    {
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}