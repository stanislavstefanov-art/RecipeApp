namespace Recipes.Application.Expenses.ListExpenses;

public sealed record ExpenseDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string Description,
    int SourceType,
    Guid? SourceReferenceId,
    IReadOnlyList<ExpenseItemDto> Items);

public sealed record ExpenseItemDto(
    Guid Id,
    string Description,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? TotalPrice);
