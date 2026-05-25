namespace Recipes.Application.Expenses.GetMonthlyExpenseReport;

public sealed record MonthlyExpenseReportDto(
    int Year,
    int Month,
    decimal TotalAmount,
    string Currency,
    int ExpenseCount,
    decimal AverageExpenseAmount,
    int? TopCategory,
    decimal FoodPercentage,
    MonthlyExpenseLargestItemDto? LargestExpense,
    IReadOnlyList<MonthlyExpenseCategoryBreakdownDto> ByCategory);

public sealed record MonthlyExpenseCategoryBreakdownDto(
    int Category,
    decimal TotalAmount,
    int Count,
    decimal Percentage);

public sealed record MonthlyExpenseLargestItemDto(
    decimal Amount,
    string Description,
    DateOnly ExpenseDate,
    int Category);