using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Products.CreateProduct;
using Recipes.Application.Products.ListProducts;

namespace Recipes.Api.Endpoints;

public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListProductsQuery(), ct);
            return result.ToHttpResult(products => Results.Ok(products));
        });

        group.MapPost("/", async (CreateProductRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateProductCommand(request.Name), ct);
            return result.ToHttpResult(product => Results.Created($"/api/products/{product.Id}", product));
        });

        return app;
    }
}

public sealed record CreateProductRequest(string Name);