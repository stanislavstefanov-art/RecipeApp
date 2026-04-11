using FluentAssertions;
using Recipes.Application.ShoppingLists.MarkShoppingListItemPurchased;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.ShoppingLists.MarkShoppingListItemPurchased;

public sealed class MarkShoppingListItemPurchasedHandlerTests
{
    [Fact]
    public async Task Should_Mark_Item_As_Purchased()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");
        shoppingList.AddItem(product, 2, "pcs");
        var itemId = shoppingList.Items.Single().Id;

        var repository = new FakeShoppingListRepository([shoppingList]);
        var handler = new MarkShoppingListItemPurchasedHandler(repository);

        var result = await handler.Handle(
            new MarkShoppingListItemPurchasedCommand(shoppingList.Id.Value, itemId.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        shoppingList.Items.Single().IsPurchased.Should().BeTrue();
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