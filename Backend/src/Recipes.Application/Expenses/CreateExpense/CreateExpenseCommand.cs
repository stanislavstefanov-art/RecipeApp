using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.CreateExpense;

public sealed record CreateExpenseCommand(
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string Description,
    int SourceType,
    Guid? SourceReferenceId) : IRequest<ErrorOr<CreateExpenseResponse>>;