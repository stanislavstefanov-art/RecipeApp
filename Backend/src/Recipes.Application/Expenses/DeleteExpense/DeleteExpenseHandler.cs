using ErrorOr;
using MediatR;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.DeleteExpense;

public sealed class DeleteExpenseHandler : IRequestHandler<DeleteExpenseCommand, ErrorOr<Deleted>>
{
    private readonly IExpenseRepository _repository;

    public DeleteExpenseHandler(IExpenseRepository repository) => _repository = repository;

    public async Task<ErrorOr<Deleted>> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var id = ExpenseId.From(request.Id);
        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity is null)
            return Error.NotFound("Expense.NotFound", $"Expense '{request.Id}' was not found.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
