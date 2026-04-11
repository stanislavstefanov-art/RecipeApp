using ErrorOr;
using MediatR;

namespace Recipes.Application.Products.CreateProduct;

public sealed record CreateProductCommand(string Name) : IRequest<ErrorOr<CreateProductResponse>>;