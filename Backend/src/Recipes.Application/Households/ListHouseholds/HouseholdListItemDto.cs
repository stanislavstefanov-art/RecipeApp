namespace Recipes.Application.Households.ListHouseholds;

public sealed record HouseholdListItemDto(
    Guid Id,
    string Name,
    int MemberCount);