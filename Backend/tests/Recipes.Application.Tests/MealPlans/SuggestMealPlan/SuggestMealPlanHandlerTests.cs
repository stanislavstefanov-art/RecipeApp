using FluentAssertions;
using Recipes.Application.Common;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.AI.Claude.Services.Stubs;

namespace Recipes.Application.Tests.MealPlans.SuggestMealPlan;

public sealed class SuggestMealPlanHandlerTests
{
    [Fact]
    public async Task Should_Return_Suggestion_When_Recipes_Exist()
    {
        var person = new Person("Stanislav");
        var household = new Household("Family");
        household.AddPerson(person);

        var recipes = new List<Recipe>
        {
            new("Pasta"),
            new("Soup")
        };

        var recipeRepository = new FakeRecipeRepository(recipes);
        var householdRepository = new FakeHouseholdRepository([household]);
        var personRepository = new FakePersonRepository([person]);
        var cookingLogRepository = new FakeCookingLogRepository();
        var pantryRepository = new FakePantryRepository();
        var suggestionService = new StubMealPlanSuggestionService();
        var currentUser = new FakeCurrentUser();

        var handler = new SuggestMealPlanHandler(
            recipeRepository,
            householdRepository,
            personRepository,
            cookingLogRepository,
            pantryRepository,
            suggestionService,
            currentUser,
            TimeProvider.System);

        var result = await handler.Handle(
            new SuggestMealPlanCommand(
                "Weekly plan",
                household.Id.Value,
                new DateOnly(2026, 4, 21),
                2,
                [3]),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Entries.Should().NotBeEmpty();
        result.Value.Entries.SelectMany(x => x.Assignments).Should().OnlyContain(x => x.PortionMultiplier > 0);
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        private readonly List<Recipe> _recipes;
        public FakeRecipeRepository(IEnumerable<Recipe> recipes) => _recipes = recipes.ToList();
        public Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_recipes.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes);
        public Task<IReadOnlyList<Recipe>> GetByHouseholdIdsAsync(IReadOnlyList<HouseholdId> householdIds, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes);

        public Task<IReadOnlyList<Recipe>> SearchByIngredientNameAsync(string ingredientName, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes.Where(x => x.Ingredients.Any(i => i.Name.Contains(ingredientName, StringComparison.OrdinalIgnoreCase))).ToList());
        public void Add(Recipe recipe) => _recipes.Add(recipe);
        public void Remove(Recipe recipe) => _recipes.Remove(recipe);
        public void RemoveRange(IEnumerable<Recipe> recipes) => _recipes.RemoveAll(recipes.Contains);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeHouseholdRepository : IHouseholdRepository
    {
        private readonly List<Household> _households;
        public FakeHouseholdRepository(IEnumerable<Household> households) => _households = households.ToList();
        public Task<Household?> GetByIdAsync(HouseholdId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_households.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Household>)_households);
        public Task<IReadOnlyList<Household>> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Household>)_households);

        public Task AddAsync(Household household, CancellationToken cancellationToken = default) { _households.Add(household); return Task.CompletedTask; }
        public void Remove(Household household) => _households.Remove(household);
        public void RemoveRange(IEnumerable<Household> households) => _households.RemoveAll(households.Contains);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePersonRepository : IPersonRepository
    {
        private readonly List<Person> _persons;
        public FakePersonRepository(IEnumerable<Person> persons) => _persons = persons.ToList();
        public Task<Person?> GetByIdAsync(PersonId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_persons.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Person>)_persons);
        public Task<IReadOnlyList<Person>> GetByHouseholdIdsAsync(IReadOnlyList<HouseholdId> householdIds, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Person>)_persons);

        public Task<IReadOnlyList<Person>> GetByIdsAsync(IEnumerable<PersonId> ids, CancellationToken cancellationToken = default)
        {
            var idSet = ids.ToHashSet();
            return Task.FromResult((IReadOnlyList<Person>)_persons.Where(x => idSet.Contains(x.Id)).ToList());
        }
        public Task AddAsync(Person person, CancellationToken cancellationToken = default) { _persons.Add(person); return Task.CompletedTask; }
        public void Remove(Person person) => _persons.Remove(person);
        public void RemoveRange(IEnumerable<Person> persons) => _persons.RemoveAll(persons.Contains);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCookingLogRepository : ICookingLogRepository
    {
        public void Add(CookingLogEntry entry) { }
        public void Remove(CookingLogEntry entry) { }
        public Task<CookingLogEntry?> GetByIdAsync(CookingLogEntryId id, CancellationToken cancellationToken = default) => Task.FromResult<CookingLogEntry?>(null);
        public Task<IReadOnlyList<CookingLogEntry>> GetByRecipeAndUserAsync(RecipeId recipeId, UserId userId, int limit, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CookingLogEntry>>([]);
        public Task<IReadOnlyList<CookingLogEntry>> GetAllByUserAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CookingLogEntry>>([]);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakePantryRepository : IPantryRepository
    {
        public Task AddAsync(PantryItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(PantryItem item) { }
        public Task<PantryItem?> GetByIdAsync(PantryItemId id, CancellationToken cancellationToken = default) => Task.FromResult<PantryItem?>(null);
        public Task<IReadOnlyList<PantryItem>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PantryItem>>([]);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public UserId UserId { get; } = UserId.New();
        public Task<IReadOnlyList<HouseholdId>> GetHouseholdIdsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<HouseholdId>>([]);
        public void InvalidateHouseholdCache() { }
    }
}