using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.MealPlans.AcceptMealPlanSuggestion;
using Recipes.Application.MealPlans.AddRecipeToMealPlan;
using Recipes.Application.MealPlans.CreateMealPlan;
using Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;
using Recipes.Application.MealPlans.GetMealPlan;
using Recipes.Application.MealPlans.ListMealPlans;
using Recipes.Application.MealPlans.RegenerateShoppingListFromMealPlan;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Application.MealPlans.UpdateMealPlanPersonAssignment;

namespace Recipes.Api.Endpoints;

public static class MealPlansEndpoints
{
    public static IEndpointRouteBuilder MapMealPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meal-plans")
            .WithTags("MealPlans");

        group.MapPost("/", async (CreateMealPlanRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateMealPlanCommand(request.Name, request.HouseholdId), ct);
            return result.ToHttpResult(response => Results.Created($"/api/meal-plans/{response.Id}", response));
        });

        group.MapGet("/{mealPlanId:guid}", async (Guid mealPlanId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMealPlanQuery(mealPlanId), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/{mealPlanId:guid}/entries", async (Guid mealPlanId, AddMealPlanEntryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddRecipeToMealPlanCommand(
                    mealPlanId,
                    request.RecipeId,
                    request.PlannedDate,
                    request.MealType,
                    request.Scope,
                    request.Assignments.Select(x => new MealPlanPersonAssignmentInputDto(
                    x.PersonId,
                    x.AssignedRecipeId,
                    x.RecipeVariationId,
                    x.PortionMultiplier,
                    x.Notes)).ToList()),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{mealPlanId:guid}/shopping-lists/{shoppingListId:guid}", async (
            Guid mealPlanId,
            Guid shoppingListId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GenerateShoppingListFromMealPlanCommand(mealPlanId, shoppingListId),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/suggest", async (SuggestMealPlanRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SuggestMealPlanCommand(
                    request.Name,
                    request.HouseholdId,
                    request.StartDate,
                    request.NumberOfDays,
                    request.MealTypes),
                ct);

            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/accept-suggestion", async (AcceptMealPlanSuggestionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AcceptMealPlanSuggestionCommand(
                    request.Name,
                    request.HouseholdId,
                    request.Entries.Select(x => new AcceptMealPlanSuggestionEntryDto(
                        x.BaseRecipeId,
                        x.PlannedDate,
                        x.MealType,
                        x.Scope,
                        x.Assignments.Select(a => new AcceptMealPlanSuggestionAssignmentDto(
                            a.PersonId,
                            a.AssignedRecipeId,
                            a.RecipeVariationId,
                            a.PortionMultiplier,
                            a.Notes)).ToList())).ToList()),
                ct);

            return result.ToHttpResult(response => Results.Created($"/api/meal-plans/{response.MealPlanId}", response));
        });

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListMealPlansQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPut("/{mealPlanId:guid}/entries/{mealPlanEntryId:guid}/assignments", async (
            Guid mealPlanId,
            Guid mealPlanEntryId,
            UpdateMealPlanPersonAssignmentRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateMealPlanPersonAssignmentCommand(
                    mealPlanId,
                    mealPlanEntryId,
                    request.PersonId,
                    request.AssignedRecipeId,
                    request.RecipeVariationId,
                    request.PortionMultiplier,
                    request.Notes),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{mealPlanId:guid}/shopping-lists/{shoppingListId:guid}/regenerate", async (
            Guid mealPlanId,
            Guid shoppingListId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RegenerateShoppingListFromMealPlanCommand(mealPlanId, shoppingListId),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

public sealed record CreateMealPlanRequest(string Name, Guid HouseholdId);
public sealed record AddMealPlanEntryRequest(Guid RecipeId, DateOnly PlannedDate, int MealType, int Scope, IReadOnlyList<AddMealPlanEntryAssignmentRequest> Assignments);
public sealed record AddMealPlanEntryAssignmentRequest(Guid PersonId, Guid AssignedRecipeId, Guid? RecipeVariationId, decimal PortionMultiplier, string? Notes);
public sealed record SuggestMealPlanRequest(string Name, Guid HouseholdId, DateOnly StartDate, int NumberOfDays, IReadOnlyList<int> MealTypes);
public sealed record AcceptMealPlanSuggestionRequest(string Name, Guid HouseholdId, IReadOnlyList<AcceptMealPlanSuggestionEntryRequest> Entries);
public sealed record AcceptMealPlanSuggestionEntryRequest(Guid BaseRecipeId, DateOnly PlannedDate, int MealType, int Scope, IReadOnlyList<AcceptMealPlanSuggestionAssignmentRequest> Assignments);
public sealed record AcceptMealPlanSuggestionAssignmentRequest(Guid PersonId, Guid AssignedRecipeId, Guid? RecipeVariationId, decimal PortionMultiplier, string? Notes);
public sealed record UpdateMealPlanPersonAssignmentRequest(Guid PersonId, Guid AssignedRecipeId, Guid? RecipeVariationId, decimal PortionMultiplier, string? Notes);