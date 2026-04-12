using FluentAssertions;
using Recipes.Application.MealPlans.CreateMealPlan;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.MealPlans.CreateMealPlan;

public sealed class CreateMealPlanHandlerTests
{
    [Fact]
    public async Task Should_Create_MealPlan()
    {
        var household = new Household("Family");
        var mealPlanRepository = new FakeMealPlanRepository();
        var householdRepository = new FakeHouseholdRepository([household]);

        var handler = new CreateMealPlanHandler(mealPlanRepository, householdRepository);

        var result = await handler.Handle(
            new CreateMealPlanCommand("Weekly meals", household.Id.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Weekly meals");
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        private readonly List<MealPlan> _mealPlans = [];

        public Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_mealPlans.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
        {
            _mealPlans.Add(mealPlan);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<MealPlan>)_mealPlans);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeHouseholdRepository : IHouseholdRepository
    {
        private readonly List<Household> _households;

        public FakeHouseholdRepository(IEnumerable<Household> households)
        {
            _households = households.ToList();
        }

        public Task<Household?> GetByIdAsync(HouseholdId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_households.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Household>)_households);

        public Task AddAsync(Household household, CancellationToken cancellationToken = default)
        {
            _households.Add(household);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}