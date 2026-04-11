using FluentAssertions;
using Recipes.Domain.Entities;

namespace Recipes.Domain.Tests.Entities;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_Should_Set_Name()
    {
        var product = new Product("Tomato");

        product.Name.Should().Be("Tomato");
    }

    [Fact]
    public void Rename_Should_Trim_Name()
    {
        var product = new Product("Tomato");

        product.Rename("  Olive Oil  ");

        product.Name.Should().Be("Olive Oil");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Empty()
    {
        var action = () => new Product(" ");

        action.Should().Throw<ArgumentException>();
    }
}