using FluentAssertions;
using Recipes.Application.ShoppingLists.MarkShoppingListItemPending;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.ShoppingLists.MarkShoppingListItemPending;

public sealed class MarkShoppingListItemPendingHandlerTests
{
    [Fact]
    public async Task Should_Mark_Item_As_Pending()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");
        shoppingList.AddItem(product, 2, "pcs");
        var itemId = shoppingList.Items.Single().Id;
        shoppingList.MarkItemPurchased(itemId);

        var repository = new FakeShoppingListRepository([shoppingList]);
        var handler = new MarkShoppingListItemPendingHandler(repository);

        var result = await handler.Handle(
            new MarkShoppingListItemPendingCommand(shoppingList.Id.Value, itemId.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        shoppingList.Items.Single().IsPurchased.Should().BeFalse();
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
}