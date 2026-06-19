namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class CookingLogPreparer
{
    public PersonId PersonId { get; private set; }

    private CookingLogPreparer() { }

    internal CookingLogPreparer(PersonId personId)
    {
        PersonId = personId;
    }
}
