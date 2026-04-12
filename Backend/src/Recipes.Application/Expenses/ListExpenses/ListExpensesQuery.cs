using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.ListExpenses;

public sealed record ListExpensesQuery() : IRequest<ErrorOr<IReadOnlyList<ExpenseDto>>>;