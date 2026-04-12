using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.ListExpenses;

public sealed class ListExpensesHandler
    : IRequestHandler<ListExpensesQuery, ErrorOr<IReadOnlyList<ExpenseDto>>>
{
    private readonly IExpenseRepository _expenseRepository;

    public ListExpensesHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<ExpenseDto>>> Handle(
        ListExpensesQuery request,
        CancellationToken cancellationToken)
    {
        var expenses = await _expenseRepository.GetAllAsync(cancellationToken);

        return expenses.Select(x => new ExpenseDto(
            x.Id.Value,
            x.Amount,
            x.Currency,
            x.ExpenseDate,
            (int)x.Category,
            x.Description,
            (int)x.SourceType,
            x.SourceReferenceId)).ToList();
    }
}