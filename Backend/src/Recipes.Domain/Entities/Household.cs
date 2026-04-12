namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class Household : Entity
{
    private readonly List<HouseholdMember> _members = [];

    public HouseholdId Id { get; private set; } = HouseholdId.New();
    public string Name { get; private set; } = string.Empty;
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

    public void AddMember(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);

        if (_members.Any(x => x.PersonId == person.Id))
        {
            return;
        }

        _members.Add(new HouseholdMember(Id, person.Id));
    }

    public void RemoveMember(PersonId personId)
    {
        var member = _members.SingleOrDefault(x => x.PersonId == personId);
        if (member is null)
        {
            return;
        }

        _members.Remove(member);
    }
}