using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Products.CreateProduct;

public sealed class CreateProductHandler
    : IRequestHandler<CreateProductCommand, ErrorOr<CreateProductResponse>>
{
    private readonly IProductRepository _productRepository;

    public CreateProductHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ErrorOr<CreateProductResponse>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var existing = await _productRepository.GetByNameAsync(normalizedName, cancellationToken);
        if (existing is not null)
        {
            return Error.Conflict(
                code: "Product.AlreadyExists",
                description: $"Product '{normalizedName}' already exists.");
        }

        var product = new Product(normalizedName);

        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        return new CreateProductResponse(product.Id.Value, product.Name);
    }
}