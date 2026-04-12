using FluentAssertions;
using Recipes.Domain.Entities;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Tests.Entities;

public sealed class ShoppingListTests
{
    [Fact]
    public void Constructor_Should_Raise_ShoppingListCreated()
    {
        var shoppingList = new ShoppingList("Weekend groceries");

        shoppingList.DomainEvents.Should().ContainSingle(x => x is ShoppingListCreated);
    }

    [Fact]
    public void AddItem_Should_Add_New_Item()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");

        shoppingList.AddItem(product, 2, "pcs");

        shoppingList.Items.Should().HaveCount(1);
        shoppingList.Items.Single().ProductName.Should().Be("Tomato");
        shoppingList.Items.Single().Quantity.Should().Be(2);
    }

    [Fact]
    public void AddItem_Should_Merge_Same_Product_And_Unit()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");

        shoppingList.AddItem(product, 2, "pcs");
        shoppingList.AddItem(product, 3, "pcs");

        shoppingList.Items.Should().HaveCount(1);
        shoppingList.Items.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public void MarkItemPurchased_Should_Set_Flag()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");

        shoppingList.AddItem(product, 2, "pcs");
        var itemId = shoppingList.Items.Single().Id;

        shoppingList.MarkItemPurchased(itemId);

        shoppingList.Items.Single().IsPurchased.Should().BeTrue();
    }

    [Fact]
    public void RemoveItem_Should_Remove_Item()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product = new Product("Tomato");

        shoppingList.AddItem(product, 2, "pcs");
        var itemId = shoppingList.Items.Single().Id;

        shoppingList.RemoveItem(itemId);

        shoppingList.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveGeneratedItems_Should_Remove_Only_Matching_Source_Items()
    {
        var shoppingList = new ShoppingList("Weekend groceries");
        var product1 = new Product("Tomato");
        var product2 = new Product("Onion");

        var mealPlanId = Guid.NewGuid();

        shoppingList.AddItem(product1, 2, "pcs", null, Recipes.Domain.Enums.ShoppingListItemSourceType.MealPlan, mealPlanId);
        shoppingList.AddItem(product2, 1, "pcs", null, Recipes.Domain.Enums.ShoppingListItemSourceType.Manual, null);

        shoppingList.RemoveGeneratedItems(Recipes.Domain.Enums.ShoppingListItemSourceType.MealPlan, mealPlanId);

        shoppingList.Items.Should().HaveCount(1);
        shoppingList.Items.Single().ProductName.Should().Be("Onion");
    }
}