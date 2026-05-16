using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.DeleteExpense;

public sealed record DeleteExpenseCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
