using ErrorOr;
using MediatR;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Products.ListProducts;

public sealed class ListProductsHandler
    : IRequestHandler<ListProductsQuery, ErrorOr<IReadOnlyList<ProductDto>>>
{
    private readonly IProductRepository _productRepository;

    public ListProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<IReadOnlyList<ProductDto>>> Handle(
        ListProductsQuery request,
        CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        var result = products
            .Select(x => new ProductDto(x.Id.Value, x.Name))
            .ToList();

        return result;
    }
}