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
        var repository = new FakeMealPlanRepository();
        var handler = new CreateMealPlanHandler(repository);

        var result = await handler.Handle(new CreateMealPlanCommand("Weekly meals"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Weekly meals");
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        private readonly List<MealPlan> _mealPlans = new();

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
}