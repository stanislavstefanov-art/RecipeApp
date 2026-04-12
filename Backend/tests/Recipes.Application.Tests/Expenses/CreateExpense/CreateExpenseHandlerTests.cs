using FluentAssertions;
using Recipes.Application.Expenses.CreateExpense;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.Expenses.CreateExpense;

public sealed class CreateExpenseHandlerTests
{
    [Fact]
    public async Task Should_Create_Expense()
    {
        var repository = new FakeExpenseRepository();
        var handler = new CreateExpenseHandler(repository);

        var result = await handler.Handle(
            new CreateExpenseCommand(
                25.40m,
                "BGN",
                new DateOnly(2026, 4, 21),
                1,
                "Groceries",
                1,
                null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        repository.Stored.Should().HaveCount(1);
    }

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        public List<Expense> Stored { get; } = [];

        public Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken = default)
            => Task.FromResult(Stored.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            Stored.Add(expense);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Expense>)Stored);

        public Task<IReadOnlyList<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Expense>)Stored
                .Where(x => x.ExpenseDate.Year == year && x.ExpenseDate.Month == month)
                .ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}