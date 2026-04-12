namespace Recipes.Application.Households.GetHousehold;

public sealed record HouseholdDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<HouseholdMemberDto> Members);

public sealed record HouseholdMemberDto(
    Guid PersonId,
    string PersonName,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes);