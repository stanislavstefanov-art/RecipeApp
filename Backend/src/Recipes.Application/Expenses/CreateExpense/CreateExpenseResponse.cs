namespace Recipes.Application.Expenses.CreateExpense;

public sealed record CreateExpenseResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string Description);