using FluentAssertions;
using Recipes.Domain.Entities;
using Recipes.Domain.Events;

namespace Recipes.Domain.Tests.Entities;

public class RecipeTests
{
    // --- Construction ---

    [Fact]
    public void Constructor_WithValidName_SetsName()
    {
        var recipe = new Recipe("Pasta");

        recipe.Name.Value.Should().Be("Pasta");
    }

    [Fact]
    public void Constructor_WithValidName_RaisesRecipeCreatedEvent()
    {
        var recipe = new Recipe("Pasta");

        recipe.DomainEvents.OfType<RecipeCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Constructor_ShouldNotRaiseRecipeRenamedEvent()
    {
        var recipe = new Recipe("Pasta");

        recipe.DomainEvents.OfType<RecipeRenamed>().Should().BeEmpty();
    }

    // --- Rename ---

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var recipe = new Recipe("Pasta");
        recipe.ClearDomainEvents();

        recipe.Rename("Risotto");

        recipe.Name.Value.Should().Be("Risotto");
    }

    [Fact]
    public void Rename_RaisesRecipeRenamedEvent()
    {
        var recipe = new Recipe("Pasta");
        recipe.ClearDomainEvents();

        recipe.Rename("Risotto");

        recipe.DomainEvents.OfType<RecipeRenamed>().Should().ContainSingle()
            .Which.NewName.Value.Should().Be("Risotto");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyName_ThrowsArgumentException(string name)
    {
        var recipe = new Recipe("Pasta");

        var act = () => recipe.Rename(name);

        act.Should().Throw<ArgumentException>();
    }

    // --- AddIngredient ---

    [Fact]
    public void AddIngredient_WithValidData_AddsToCollection()
    {
        var recipe = new Recipe("Pasta");

        recipe.AddIngredient("Salt", 1m, "tsp");

        recipe.Ingredients.Should().ContainSingle(i => i.Name == "Salt");
    }

    [Fact]
    public void AddIngredient_RaisesIngredientAddedEvent()
    {
        var recipe = new Recipe("Pasta");
        recipe.ClearDomainEvents();

        recipe.AddIngredient("Salt", 1m, "tsp");

        recipe.DomainEvents.OfType<RecipeIngredientAdded>().Should().ContainSingle()
            .Which.Name.Should().Be("Salt");
    }

    [Fact]
    public void AddIngredient_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var recipe = new Recipe("Pasta");

        var act = () => recipe.AddIngredient("Salt", -1m, "tsp");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddIngredient_WithEmptyName_ThrowsArgumentException(string name)
    {
        var recipe = new Recipe("Pasta");

        var act = () => recipe.AddIngredient(name, 1m, "tsp");

        act.Should().Throw<ArgumentException>();
    }

    // --- AddStep ---

    [Fact]
    public void AddStep_FirstStep_HasOrderOne()
    {
        var recipe = new Recipe("Pasta");

        recipe.AddStep("Boil water");

        recipe.Steps.Single().Order.Should().Be(1);
    }

    [Fact]
    public void AddStep_MultipleSteps_OrdersSequentially()
    {
        var recipe = new Recipe("Pasta");

        recipe.AddStep("Boil water");
        recipe.AddStep("Add pasta");

        recipe.Steps.Select(s => s.Order).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void AddStep_RaisesStepAddedEvent()
    {
        var recipe = new Recipe("Pasta");
        recipe.ClearDomainEvents();

        recipe.AddStep("Boil water");

        recipe.DomainEvents.OfType<StepAdded>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddStep_WithEmptyInstruction_ThrowsArgumentException(string instruction)
    {
        var recipe = new Recipe("Pasta");

        var act = () => recipe.AddStep(instruction);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddVariation_Should_Add_Variation()
    {
        var recipe = new Recipe("Gyuvetche");

        var variation = recipe.AddVariation(
            "Vegetarian",
            "No meat version",
            "Remove meat and add more potatoes");

        recipe.Variations.Should().ContainSingle();
        variation.Name.Should().Be("Vegetarian");
    }

    [Fact]
    public void Variation_Should_Support_Remove_And_Override_Ingredient()
    {
        var recipe = new Recipe("Gyuvetche");
        var variation = recipe.AddVariation("Vegetarian", "No meat", "Remove meat and add more potatoes");

        variation.RemoveIngredient("Meat");
        variation.OverrideIngredient("Potatoes", 500, "g");

        variation.IngredientOverrides.Should().HaveCount(2);
        variation.IngredientOverrides.Should().Contain(x => x.IngredientName == "Meat" && x.IsRemoved);
        variation.IngredientOverrides.Should().Contain(x => x.IngredientName == "Potatoes" && x.Quantity == 500);
    }
}
