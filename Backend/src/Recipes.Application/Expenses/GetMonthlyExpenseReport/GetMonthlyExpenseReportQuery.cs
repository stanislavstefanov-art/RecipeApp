using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.GetMonthlyExpenseReport;

public sealed record GetMonthlyExpenseReportQuery(int Year, int Month)
    : IRequest<ErrorOr<MonthlyExpenseReportDto>>;