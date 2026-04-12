using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.ShoppingLists.AddItemToShoppingList;
using Recipes.Application.ShoppingLists.AddRecipeToShoppingList;
using Recipes.Application.ShoppingLists.AddRecipesToShoppingList;
using Recipes.Application.ShoppingLists.CreateShoppingList;
using Recipes.Application.ShoppingLists.GetShoppingList;
using Recipes.Application.ShoppingLists.ListShoppingLists;
using Recipes.Application.ShoppingLists.MarkShoppingListItemPending;
using Recipes.Application.ShoppingLists.MarkShoppingListItemPurchased;
using Recipes.Application.ShoppingLists.PurchaseShoppingListItem;

namespace Recipes.Api.Endpoints;

public static class ShoppingListsEndpoints
{
    public static IEndpointRouteBuilder MapShoppingListsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shopping-lists")
            .WithTags("ShoppingLists");

        group.MapPost("/", async (CreateShoppingListRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateShoppingListCommand(request.Name), ct);
            return result.ToHttpResult(response => Results.Created($"/api/shopping-lists/{response.Id}", response));
        });

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListShoppingListsQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/{shoppingListId:guid}/items", async (Guid shoppingListId, AddShoppingListItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddItemToShoppingListCommand(
                    shoppingListId,
                    request.ProductId,
                    request.Quantity,
                    request.Unit),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{shoppingListId:guid}/recipes/{recipeId:guid}", async (Guid shoppingListId, Guid recipeId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddRecipeToShoppingListCommand(shoppingListId, recipeId),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{shoppingListId:guid}/items/{shoppingListItemId:guid}/purchase", async (
            Guid shoppingListId,
            Guid shoppingListItemId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new MarkShoppingListItemPurchasedCommand(shoppingListId, shoppingListItemId),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{shoppingListId:guid}/items/{shoppingListItemId:guid}/pending", async (
            Guid shoppingListId,
            Guid shoppingListItemId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new MarkShoppingListItemPendingCommand(shoppingListId, shoppingListItemId),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapGet("/{shoppingListId:guid}", async (
            Guid shoppingListId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetShoppingListQuery(shoppingListId), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/{shoppingListId:guid}/recipes", async (
            Guid shoppingListId,
            AddRecipesToShoppingListRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddRecipesToShoppingListCommand(shoppingListId, request.RecipeIds),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{shoppingListId:guid}/items/{shoppingListItemId:guid}/purchase-with-expense", async (
            Guid shoppingListId,
            Guid shoppingListItemId,
            PurchaseShoppingListItemRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new PurchaseShoppingListItemCommand(
                    shoppingListId,
                    shoppingListItemId,
                    request.Amount,
                    request.Currency,
                    request.ExpenseDate,
                    request.Description),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

public sealed record CreateShoppingListRequest(string Name);
public sealed record AddShoppingListItemRequest(Guid ProductId, decimal Quantity, string Unit);
public sealed record AddRecipesToShoppingListRequest(IReadOnlyList<Guid> RecipeIds);
public sealed record PurchaseShoppingListItemRequest(decimal Amount, string Currency, DateOnly ExpenseDate, string? Description);