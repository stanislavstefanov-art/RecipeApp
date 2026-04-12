namespace Recipes.Application.Persons.ListPersons;

public sealed record PersonDto(
    Guid Id,
    string Name,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes);