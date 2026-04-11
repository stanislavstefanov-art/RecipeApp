using FluentAssertions;
using Recipes.Application.Products.CreateProduct;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Tests.Products.CreateProduct;

public sealed class CreateProductHandlerTests
{
    [Fact]
    public async Task Should_Create_Product_When_Name_Is_New()
    {
        var repository = new FakeProductRepository();
        var handler = new CreateProductHandler(repository);

        var result = await handler.Handle(new CreateProductCommand("Tomato"), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Tomato");
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Product_Already_Exists()
    {
        var repository = new FakeProductRepository();
        await repository.AddAsync(new Product("Tomato"));

        var handler = new CreateProductHandler(repository);

        var result = await handler.Handle(new CreateProductCommand("Tomato"), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Product.AlreadyExists");
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
            => Task.FromResult((IReadOnlyList<Product>)_products.OrderBy(x => x.Name).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}