namespace Recipes.Domain.Repositories;

using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}