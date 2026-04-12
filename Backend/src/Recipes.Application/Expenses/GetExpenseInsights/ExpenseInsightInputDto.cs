using Recipes.Application.Expenses.GetMonthlyExpenseReport;
using Recipes.Application.Expenses.ListExpenses;

namespace Recipes.Application.Expenses.GetExpenseInsights;

public sealed record ExpenseInsightInputDto(
    MonthlyExpenseReportDto MonthlyReport,
    IReadOnlyList<ExpenseDto> Expenses);