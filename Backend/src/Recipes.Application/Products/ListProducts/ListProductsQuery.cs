using ErrorOr;
using MediatR;

namespace Recipes.Application.Products.ListProducts;

public sealed record ListProductsQuery() : IRequest<ErrorOr<IReadOnlyList<ProductDto>>>;