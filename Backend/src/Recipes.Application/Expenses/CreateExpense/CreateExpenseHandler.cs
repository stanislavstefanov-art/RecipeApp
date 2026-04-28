using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.CreateExpense;

public sealed class CreateExpenseHandler
    : IRequestHandler<CreateExpenseCommand, ErrorOr<CreateExpenseResponse>>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly ICurrentUser _currentUser;

    public CreateExpenseHandler(IExpenseRepository expenseRepository, ICurrentUser currentUser)
    {
        _expenseRepository = expenseRepository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<CreateExpenseResponse>> Handle(
        CreateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var householdId = HouseholdId.From(request.HouseholdId);
        var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        if (!memberIds.Contains(householdId))
        {
            return Error.Forbidden("Expense.HouseholdAccessDenied", "You are not a member of the specified household.");
        }

        var expense = new Expense(
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            (ExpenseCategory)request.Category,
            request.Description,
            (ExpenseSourceType)request.SourceType,
            request.SourceReferenceId,
            householdId);

        await _expenseRepository.AddAsync(expense, cancellationToken);
        await _expenseRepository.SaveChangesAsync(cancellationToken);

        return new CreateExpenseResponse(
            expense.Id.Value,
            expense.Amount,
            expense.Currency,
            expense.ExpenseDate,
            (int)expense.Category,
            expense.Description);
    }
}