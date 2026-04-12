namespace Recipes.Application.Expenses.ListExpenses;

public sealed record ExpenseDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string Description,
    int SourceType,
    Guid? SourceReferenceId);