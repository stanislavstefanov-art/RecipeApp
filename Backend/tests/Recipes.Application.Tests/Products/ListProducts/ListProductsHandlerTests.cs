using FluentAssertions;
using Recipes.Application.Products.ListProducts;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.Products.ListProducts;

public sealed class ListProductsHandlerTests
{
    [Fact]
    public async Task Should_Return_All_Products()
    {
        var repository = new FakeProductRepository(
        [
            new Product("Tomato"),
            new Product("Eggs")
        ]);

        var handler = new ListProductsHandler(repository);

        var result = await handler.Handle(new ListProductsQuery(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
        result.Value.Select(x => x.Name).Should().Contain(["Tomato", "Eggs"]);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly IReadOnlyList<Product> _products;

        public FakeProductRepository(IReadOnlyList<Product> products)
        {
            _products = products;
        }

        public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Id == id));

        public Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(_products.SingleOrDefault(x => x.Name == name));

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_products);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}