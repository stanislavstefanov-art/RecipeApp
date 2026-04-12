using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.GetExpenseInsights;

public sealed record GetExpenseInsightsQuery(int Year, int Month)
    : IRequest<ErrorOr<ExpenseInsightDto>>;