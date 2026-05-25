using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.ListExpenses;

public sealed class ListExpensesHandler
    : IRequestHandler<ListExpensesQuery, ErrorOr<IReadOnlyList<ExpenseDto>>>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICurrentUser _currentUser;

    public ListExpensesHandler(IExpenseRepository expenseRepository, ICurrentUser currentUser)
    {
        _expenseRepository = expenseRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<IReadOnlyList<ExpenseDto>>> Handle(
        ListExpensesQuery request,
        CancellationToken cancellationToken)
    {
        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
        var expenses = await _expenseRepository.GetByHouseholdIdsAsync(householdIds, cancellationToken);

        return expenses.Select(x => new ExpenseDto(
            x.Id.Value,
            x.Amount,
            x.Currency,
            x.ExpenseDate,
            (int)x.Category,
            x.Description,
            (int)x.SourceType,
            x.SourceReferenceId,
            x.Items.Select(i => new ExpenseItemDto(
                i.Id.Value,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.TotalPrice)).ToList())).ToList();
    }
}