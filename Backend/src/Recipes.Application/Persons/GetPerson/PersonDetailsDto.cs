namespace Recipes.Application.Persons.GetPerson;

public sealed record PersonDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes);