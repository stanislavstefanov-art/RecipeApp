using FluentAssertions;
using Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.MealPlans.GenerateShoppingListFromMealPlan;

public sealed class GenerateShoppingListFromMealPlanHandlerTests
{
    [Fact]
    public async Task Should_Add_Ingredients_From_MealPlan_Recipes_To_ShoppingList()
    {
        var recipe1 = new Recipe("Pasta");
        recipe1.AddIngredient("Tomato", 2, "pcs");
        recipe1.AddIngredient("Garlic", 3, "cloves");

        var recipe2 = new Recipe("Soup");
        recipe2.AddIngredient("Tomato", 1, "pcs");
        recipe2.AddIngredient("Onion", 1, "pcs");

        var mealPlan = new MealPlan("Weekly dinners");
        mealPlan.AddRecipe(recipe1, new DateOnly(2026, 4, 21), MealType.Dinner);
        mealPlan.AddRecipe(recipe2, new DateOnly(2026, 4, 22), MealType.Dinner);

        var shoppingList = new ShoppingList("Weekly shopping");

        var mealPlanRepository = new FakeMealPlanRepository([mealPlan]);
        var shoppingListRepository = new FakeShoppingListRepository([shoppingList]);
        var recipeRepository = new FakeRecipeRepository([recipe1, recipe2]);
        var productRepository = new FakeProductRepository();

        var handler = new GenerateShoppingListFromMealPlanHandler(
            mealPlanRepository,
            shoppingListRepository,
            recipeRepository,
            productRepository);

        var result = await handler.Handle(
            new GenerateShoppingListFromMealPlanCommand(mealPlan.Id.Value, shoppingList.Id.Value),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        shoppingList.Items.Should().HaveCount(3);
        shoppingList.Items.Single(x => x.ProductName == "Tomato").Quantity.Should().Be(3);
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        private readonly List<MealPlan> _mealPlans;

        public FakeMealPlanRepository(IEnumerable<MealPlan> mealPlans)
        {
            _mealPlans = mealPlans.ToList();
        }

        public Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_mealPlans.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
        {
            _mealPlans.Add(mealPlan);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<MealPlan>)_mealPlans);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
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
                .Where(x => x.Ingredients.Any(i => i.Name.Contains(ingredientName, StringComparison.OrdinalIgnoreCase)))
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