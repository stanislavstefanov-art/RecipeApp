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

    public async Task<IReadOnlyList<Expense>> GetByHouseholdIdsAsync(
        IReadOnlyList<HouseholdId> householdIds,
        CancellationToken cancellationToken = default)
    {
        // EF Core can't translate any operation against a nullable strongly-typed
        // ID with a value conversion — filter client-side. Volumes are small.
        var ids = householdIds.Select(h => h.Value).ToHashSet();
        var all = await _dbContext.Expenses.ToListAsync(cancellationToken);
        return all
            .Where(x => x.HouseholdId.HasValue && ids.Contains(x.HouseholdId.Value.Value))
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Amount)
            .ToList();
    }

    public async Task<IReadOnlyList<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Expenses
            .Where(x => x.ExpenseDate.Year == year && x.ExpenseDate.Month == month)
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> GetByMonthAndHouseholdIdsAsync(
        int year,
        int month,
        IReadOnlyList<HouseholdId> householdIds,
        CancellationToken cancellationToken = default)
    {
        // Keep the year/month filter server-side (translates fine), apply the
        // nullable strongly-typed HouseholdId filter client-side (see notes above).
        var ids = householdIds.Select(h => h.Value).ToHashSet();
        var monthRows = await _dbContext.Expenses
            .Where(x => x.ExpenseDate.Year == year && x.ExpenseDate.Month == month)
            .ToListAsync(cancellationToken);
        return monthRows
            .Where(x => x.HouseholdId.HasValue && ids.Contains(x.HouseholdId.Value.Value))
            .OrderByDescending(x => x.ExpenseDate)
            .ThenByDescending(x => x.Amount)
            .ToList();
    }

    public void Remove(Expense expense) => _dbContext.Expenses.Remove(expense);

    public void RemoveRange(IEnumerable<Expense> expenses) => _dbContext.Expenses.RemoveRange(expenses);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}