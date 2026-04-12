using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly RecipesDbContext _dbContext;

    public ExpenseRepository(RecipesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _dbContext.Expenses.AddAsync(expense, cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .Where(x => x.ExpenseDate.Year == year && x.ExpenseDate.Month == month)
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}