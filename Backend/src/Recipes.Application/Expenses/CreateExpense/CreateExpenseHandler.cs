using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.CreateExpense;

public sealed class CreateExpenseHandler
    : IRequestHandler<CreateExpenseCommand, ErrorOr<CreateExpenseResponse>>
{
    private readonly IExpenseRepository _expenseRepository;

    public CreateExpenseHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public async Task<ErrorOr<CreateExpenseResponse>> Handle(
        CreateExpenseCommand request,
        CancellationToken cancellationToken)
    {
        var expense = new Expense(
            request.Amount,
            request.Currency,
            request.ExpenseDate,
            (ExpenseCategory)request.Category,
            request.Description,
            (ExpenseSourceType)request.SourceType,
            request.SourceReferenceId);

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