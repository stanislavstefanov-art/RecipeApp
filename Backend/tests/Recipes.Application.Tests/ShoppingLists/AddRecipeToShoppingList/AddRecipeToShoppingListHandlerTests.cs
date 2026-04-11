using FluentAssertions;
using Recipes.Application.ShoppingLists.AddRecipeToShoppingList;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.ShoppingLists.AddRecipeToShoppingList;

public sealed class AddRecipeToShoppingListHandlerTests
{
    [Fact]
    public async Task Should_Add_All_Recipe_Ingredients_To_Shopping_List()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var recipe = new Recipe("Pasta");
        recipe.AddIngredient("Tomato", 2, "pcs");
        recipe.AddIngredient("Garlic", 3, "cloves");

        var shoppingListRepository = new FakeShoppingListRepository([shoppingList]);
        var recipeRepository = new FakeRecipeRepository([recipe]);
        var productRepository = new FakeProductRepository();

        var handler = new AddRecipeToShoppingListHandler(
            shoppingListRepository,
            recipeRepository,
            productRepository);

        var result = await handler.Handle(
            new AddRecipeToShoppingListCommand(shoppingList.Id.Value, recipe.Id.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        shoppingList.Items.Should().HaveCount(2);
        shoppingList.Items.Select(x => x.ProductName).Should().Contain(["Tomato", "Garlic"]);
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

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        private readonly List<Recipe> _recipes;

        public FakeRecipeRepository(IEnumerable<Recipe> recipes)
        {
            _recipes = recipes.ToList();
        }

        public Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_recipes.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes);

        public Task<IReadOnlyList<Recipe>> SearchByIngredientNameAsync(string ingredientName, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes
                .Where(x => x.Ingredients.Any(i =>
                    i.Name.Contains(ingredientName, StringComparison.OrdinalIgnoreCase)))
                .ToList());

        public void Add(Recipe recipe)
        {
            _recipes.Add(recipe);
        }

        public void Remove(Recipe recipe)
        {
            _recipes.Remove(recipe);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new();

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Id == id));

        public Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Product>)_products);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}