namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class HouseholdMember
{
    public HouseholdId HouseholdId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private HouseholdMember() { }

    internal HouseholdMember(HouseholdId householdId, UserId userId, DateTimeOffset joinedAt)
    {
        HouseholdId = householdId;
        UserId = userId;
        JoinedAt = joinedAt;
    }
}
