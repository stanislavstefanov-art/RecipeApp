using FluentAssertions;
using Recipes.Application.Expenses.GetMonthlyExpenseReport;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.Expenses.GetMonthlyExpenseReport;

public sealed class GetMonthlyExpenseReportHandlerTests
{
    [Fact]
    public async Task Should_Return_Category_Breakdown()
    {
        var repository = new FakeExpenseRepository(
        [
            new Expense(20m, "BGN", new DateOnly(2026, 4, 1), ExpenseCategory.Food, "Groceries", ExpenseSourceType.Manual),
            new Expense(10m, "BGN", new DateOnly(2026, 4, 2), ExpenseCategory.Transport, "Bus", ExpenseSourceType.Manual),
            new Expense(30m, "BGN", new DateOnly(2026, 4, 3), ExpenseCategory.Food, "More groceries", ExpenseSourceType.Manual)
        ]);

        var handler = new GetMonthlyExpenseReportHandler(repository);

        var result = await handler.Handle(new GetMonthlyExpenseReportQuery(2026, 4), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.TotalAmount.Should().Be(60m);
        result.Value.TopCategory.Should().Be("Food");
        result.Value.Categories.Should().HaveCount(2);
        result.Value.ExpenseCount.Should().Be(3);
        result.Value.AverageExpenseAmount.Should().Be(20m);
        result.Value.FoodPercentage.Should().Be(83.33m);
        result.Value.LargestExpense.Should().NotBeNull();
        result.Value.LargestExpense!.Amount.Should().Be(30m);
    }

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        private readonly List<Expense> _expenses;

        public FakeExpenseRepository(IEnumerable<Expense> expenses)
        {
            _expenses = expenses.ToList();
        }

        public Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_expenses.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            _expenses.Add(expense);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Expense>)_expenses);

        public Task<IReadOnlyList<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Expense>)_expenses
                .Where(x => x.ExpenseDate.Year == year && x.ExpenseDate.Month == month)
                .ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}