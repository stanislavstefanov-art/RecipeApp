namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

public sealed class Expense : Entity
{
    public ExpenseId Id { get; private set; } = ExpenseId.New();
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateOnly ExpenseDate { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public ExpenseSourceType SourceType { get; private set; }
    public Guid? SourceReferenceId { get; private set; }

    private Expense() { }

    public Expense(
        decimal amount,
        string currency,
        DateOnly expenseDate,
        ExpenseCategory category,
        string description,
        ExpenseSourceType sourceType,
        Guid? sourceReferenceId = null)
    {
        SetAmount(amount);
        SetCurrency(currency);
        ExpenseDate = expenseDate;
        Category = category;
        SetDescription(description);
        SourceType = sourceType;
        SourceReferenceId = sourceReferenceId;

        RaiseDomainEvent(new ExpenseCreated(
            Id,
            Amount,
            Currency,
            ExpenseDate,
            Category,
            SourceType,
            SourceReferenceId));
    }

    public void UpdateDescription(string description)
    {
        SetDescription(description);
    }

    private void SetAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty.", nameof(currency));
        }

        Currency = currency.Trim().ToUpperInvariant();
    }

    private void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        }

        Description = description.Trim();
    }
}