using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Recipes.AddIngredientToRecipe;
using Recipes.Application.Recipes.CreateRecipe;
using Recipes.Application.Recipes.DeleteRecipe;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Application.Recipes.ListRecipes;
using Recipes.Application.Recipes.SearchRecipesByIngredient;
using Recipes.Application.Recipes.UpdateRecipe;

namespace Recipes.Api.Endpoints;

public static class RecipesEndpoints
{
    public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recipes")
            .WithTags("Recipes");

        group.MapPost("/", async (CreateRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateRecipeCommand(request.Name), ct);
            return result.ToHttpResult(response => Results.Created($"/api/recipes/{response.Id}", response));
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRecipeQuery(id), ct);
            return result.ToHttpResult(recipe => Results.Ok(recipe));
        });

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListRecipesQuery(), ct);
            return result.ToHttpResult(recipes => Results.Ok(recipes));
        });

        group.MapGet("/search", async (string ingredient, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SearchRecipesByIngredientQuery(ingredient), ct);
            return result.ToHttpResult(recipes => Results.Ok(recipes));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateRecipeCommand(id, request.Name), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{id:guid}/ingredients", async (Guid id, AddIngredientToRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddIngredientToRecipeCommand(id, request.Name, request.Quantity, request.Unit),
                ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteRecipeCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

public sealed record CreateRecipeRequest(string Name);

public sealed record UpdateRecipeRequest(string Name);
