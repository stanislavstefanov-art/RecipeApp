using FluentAssertions;
using Recipes.Application.ShoppingLists.PurchaseShoppingListItem;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.ShoppingLists.PurchaseShoppingListItem;

public sealed class PurchaseShoppingListItemHandlerTests
{
    [Fact]
    public async Task Should_Mark_Item_Purchased_And_Create_Expense()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");
        shoppingList.AddItem(product, 2, "pcs");
        var itemId = shoppingList.Items.Single().Id;

        var shoppingListRepository = new FakeShoppingListRepository([shoppingList]);
        var expenseRepository = new FakeExpenseRepository();

        var handler = new PurchaseShoppingListItemHandler(
            shoppingListRepository,
            expenseRepository);

        var result = await handler.Handle(
            new PurchaseShoppingListItemCommand(
                shoppingList.Id.Value,
                itemId.Value,
                5.40m,
                "BGN",
                new DateOnly(2026, 4, 21),
                null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        shoppingList.Items.Single().IsPurchased.Should().BeTrue();
        expenseRepository.Stored.Should().HaveCount(1);
        expenseRepository.Stored.Single().Amount.Should().Be(5.40m);
    }

    private sealed class FakeShoppingListRepository : IShoppingListRepository
    {
        private readonly List<ShoppingList> _shoppingLists;

        public FakeShoppingListRepository(IEnumerable<ShoppingList> shoppingLists)
        {
            _shoppingLists = shoppingLists.ToList();
        }

        public Task<ShoppingList?> GetByIdAsync(ShoppingListId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_shoppingLists.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(ShoppingList shoppingList, CancellationToken cancellationToken = default)
        {
            _shoppingLists.Add(shoppingList);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<ShoppingList>)_shoppingLists);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        public List<Expense> Stored { get; } = [];

        public Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken = default)
            => Task.FromResult(Stored.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
        {
            Stored.Add(expense);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Expense>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Expense>)Stored);

        public Task<IReadOnlyList<Expense>> GetByMonthAsync(int year, int month, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Expense>)Stored
                .Where(x => x.ExpenseDate.Year == year && x.ExpenseDate.Month == month)
                .ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}