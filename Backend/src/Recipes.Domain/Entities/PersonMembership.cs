namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class PersonMembership
{
    public HouseholdMemberId Id { get; private set; } = HouseholdMemberId.New();
    public HouseholdId HouseholdId { get; private set; }
    public PersonId PersonId { get; private set; }

    private PersonMembership() { }

    internal PersonMembership(HouseholdId householdId, PersonId personId)
    {
        HouseholdId = householdId;
        PersonId = personId;
    }
}
