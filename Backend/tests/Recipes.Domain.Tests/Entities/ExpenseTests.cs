using FluentAssertions;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Events;

namespace Recipes.Domain.Tests.Entities;

public sealed class ExpenseTests
{
    [Fact]
    public void Constructor_Should_Raise_ExpenseCreated()
    {
        var expense = new Expense(
            12.50m,
            "bgn",
            new DateOnly(2026, 4, 21),
            ExpenseCategory.Food,
            "Tomatoes",
            ExpenseSourceType.Manual);

        expense.DomainEvents.Should().ContainSingle(x => x is ExpenseCreated);
        expense.Currency.Should().Be("BGN");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Amount_Is_Invalid()
    {
        var action = () => new Expense(
            0,
            "BGN",
            new DateOnly(2026, 4, 21),
            ExpenseCategory.Food,
            "Tomatoes",
            ExpenseSourceType.Manual);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}