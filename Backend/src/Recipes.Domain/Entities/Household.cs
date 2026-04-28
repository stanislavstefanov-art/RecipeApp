namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class Household : Entity
{
    private readonly List<PersonMembership> _people = [];
    private readonly List<HouseholdMember> _members = [];

    public HouseholdId Id { get; private set; } = HouseholdId.New();
    public string Name { get; private set; } = string.Empty;

    // Person memberships (eater profiles for meal planning)
    public IReadOnlyCollection<PersonMembership> People => _people.AsReadOnly();

    // User memberships (login identities with data access)
    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();

    private Household() { }

    public Household(string name)
    {
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Household name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public void AddPerson(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        if (_people.Any(x => x.PersonId == person.Id))
        {
            return;
        }

        _people.Add(new PersonMembership(Id, person.Id));
    }

    public void RemovePerson(PersonId personId)
    {
        var membership = _people.SingleOrDefault(x => x.PersonId == personId);
        if (membership is null)
        {
            return;
        }

        _people.Remove(membership);
    }

    public void AddUser(UserId userId, DateTimeOffset joinedAt)
    {
        if (_members.Any(x => x.UserId == userId))
        {
            return;
        }

        _members.Add(new HouseholdMember(Id, userId, joinedAt));
    }

    public void RemoveUser(UserId userId)
    {
        var member = _members.SingleOrDefault(x => x.UserId == userId);
        if (member is null)
        {
            return;
        }

        _members.Remove(member);
    }
}
