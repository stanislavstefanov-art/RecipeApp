namespace Recipes.Application.Expenses.GetMonthlyExpenseReport;

public sealed record MonthlyExpenseReportDto(
    int Year,
    int Month,
    decimal TotalAmount,
    string Currency,
    int ExpenseCount,
    decimal AverageExpenseAmount,
    string? TopCategory,
    decimal FoodPercentage,
    MonthlyExpenseLargestItemDto? LargestExpense,
    IReadOnlyList<MonthlyExpenseCategoryBreakdownDto> Categories);

public sealed record MonthlyExpenseCategoryBreakdownDto(
    string Category,
    decimal Amount,
    decimal Percentage);

public sealed record MonthlyExpenseLargestItemDto(
    decimal Amount,
    string Description,
    DateOnly ExpenseDate,
    string Category);