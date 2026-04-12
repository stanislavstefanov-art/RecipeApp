namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class HouseholdMember
{
    public HouseholdMemberId Id { get; private set; } = HouseholdMemberId.New();
    public HouseholdId HouseholdId { get; private set; }
    public PersonId PersonId { get; private set; }

    private HouseholdMember() { }

    internal HouseholdMember(HouseholdId householdId, PersonId personId)
    {
        HouseholdId = householdId;
        PersonId = personId;
    }
}