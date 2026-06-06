using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Recipes.AddIngredientToRecipe;
using Recipes.Application.Recipes.AddRecipeVariation;
using Recipes.Application.Recipes.AddStepToRecipe;
using Recipes.Application.Recipes.CreateRecipe;
using Recipes.Application.Recipes.DeleteRecipe;
using Recipes.Application.Recipes.DeleteRecipeImage;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Application.Recipes.AnalyseRecipeNutrition;
using Recipes.Application.Recipes.ImportRecipeFromUrl;
using Recipes.Application.Recipes.ListRecipes;
using Recipes.Application.Recipes.SearchRecipesByIngredient;
using Recipes.Application.Recipes.SuggestIngredientSubstitutions;
using Recipes.Application.Recipes.BatchAnalyseRecipes;
using Recipes.Application.Recipes.ReviewRecipeDraft;
using Recipes.Application.Recipes.CritiqueRecipe;
using Recipes.Application.Recipes.ScaleRecipe;
using Recipes.Application.Recipes.UpdateRecipe;
using Recipes.Application.Recipes.UploadRecipeImage;
using Recipes.Application.Recipes.DeleteRecipeRating;
using Recipes.Application.Recipes.RateRecipe;
using Recipes.Application.Recipes.UpdateRecipeVariationOverrides;
using Recipes.Application.Recipes.RemoveIngredientFromRecipe;
using Recipes.Application.Recipes.RemoveStepFromRecipe;
using Recipes.Application.Recipes.SetRecipeDifficulty;
using Recipes.Application.Recipes.SetMealsPerCook;
using Recipes.Application.Recipes.SetRecipeOrigin;
using Recipes.Application.Recipes.SetRecipeType;
using Recipes.Application.Recipes.UpdateIngredientInRecipe;

namespace Recipes.Api.Endpoints;

public static class RecipesEndpoints
{
    public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/recipes")
            .WithTags("Recipes")
            .RequireAuthorization();

        group.MapPost("/", async (CreateRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateRecipeCommand(request.Name, request.HouseholdId, request.RecipeType, request.IsImported, request.DifficultyLevel, request.Origin), ct);
            return result.ToHttpResult(response => Results.Created($"/api/recipes/{response.Id}", response));
        });

        group.MapPost("/import", async (ImportRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ImportRecipeFromTextCommand(request.Text), ct);
            return result.ToHttpResult(imported => Results.Ok(imported));
        });

        group.MapPost("/import/url", async (ImportRecipeFromUrlRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ImportRecipeFromUrlCommand(request.SourceUrl), ct);
            return result.ToHttpResult(imported => Results.Ok(imported));
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

        group.MapPost("/{id:guid}/steps", async (Guid id, AddStepToRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddStepToRecipeCommand(id, request.Instruction),
                ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapDelete("/{id:guid}/ingredients/{ingredientId:guid}", async (Guid id, Guid ingredientId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveIngredientFromRecipeCommand(id, ingredientId), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPut("/{id:guid}/ingredients/{ingredientId:guid}", async (Guid id, Guid ingredientId, UpdateIngredientInRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateIngredientInRecipeCommand(id, ingredientId, request.Name, request.Quantity, request.Unit), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPut("/{id:guid}/difficulty", async (Guid id, SetRecipeDifficultyRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetRecipeDifficultyCommand(id, request.DifficultyLevel), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPut("/{id:guid}/type", async (Guid id, SetRecipeTypeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetRecipeTypeCommand(id, request.RecipeType), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPut("/{id:guid}/origin", async (Guid id, SetRecipeOriginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetRecipeOriginCommand(id, request.Origin), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPut("/{id:guid}/meals-per-cook", async (Guid id, SetMealsPerCookRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetMealsPerCookCommand(id, request.MealsPerCook), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapDelete("/{id:guid}/steps/{stepId:guid}", async (Guid id, Guid stepId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveStepFromRecipeCommand(id, stepId), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteRecipeCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{id:guid}/image", async (Guid id, IFormFile file, ISender sender, CancellationToken ct) =>
        {
            const long maxBytes = 5 * 1024 * 1024;
            if (file.Length > maxBytes)
                return Results.BadRequest("File exceeds maximum size of 5 MB.");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest("Only JPEG, PNG, and WebP images are accepted.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";

            await using var stream = file.OpenReadStream();
            var result = await sender.Send(
                new UploadRecipeImageCommand(id, stream, file.ContentType, ext), ct);

            return result.ToHttpResult(url => Results.Ok(new { imageUrl = url }));
        }).DisableAntiforgery();

        group.MapDelete("/{id:guid}/image", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteRecipeImageCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{id:guid}/ratings", async (Guid id, RateRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RateRecipeCommand(id, request.Stars, request.Comment), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapDelete("/{id:guid}/ratings", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteRecipeRatingCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{id:guid}/analyse-nutrition", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AnalyseRecipeNutritionCommand(id), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapPost("/{id:guid}/critique", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CritiqueRecipeCommand(id), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapPost("/{id:guid}/scale", async (Guid id, ScaleRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ScaleRecipeCommand(id, request.FromServings, request.ToServings), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapPost("/import/with-jury", async (ImportRecipeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ReviewRecipeDraftCommand(request.Text), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapPost("/batch-analyze", async (BatchAnalyzeRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitRecipeBatchAnalysisCommand(request.RecipeIds), ct);
            return result.ToHttpResult(dto => Results.Accepted($"/api/recipes/batch-analyze/{dto.BatchId}", dto));
        });

        group.MapGet("/batch-analyze/{batchId}", async (string batchId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRecipeBatchAnalysisResultsQuery(batchId), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapPost("/suggest-substitutions", async (SuggestIngredientSubstitutionsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SuggestIngredientSubstitutionsCommand(
                    request.IngredientName,
                    request.RecipeContext,
                    request.DietaryGoal),
                ct);

            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/{id:guid}/variations", async (Guid id, AddRecipeVariationRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddRecipeVariationCommand(
                    id,
                    request.Name,
                    request.Notes,
                    request.IngredientAdjustmentNotes),
                ct);

            return result.ToHttpResult(response => Results.Created($"/api/recipes/{id}/variations/{response.Id}", response));
        });

        group.MapPut("/{recipeId:guid}/variations/{recipeVariationId:guid}/overrides", async (
            Guid recipeId,
            Guid recipeVariationId,
            UpdateRecipeVariationOverridesRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new UpdateRecipeVariationOverridesCommand(
                    recipeId,
                    recipeVariationId,
                    request.Overrides.Select(x => new RecipeVariationIngredientOverrideDto(
                        x.IngredientName,
                        x.Quantity,
                        x.Unit,
                        x.IsRemoved)).ToList()),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

public sealed record CreateRecipeRequest(string Name, Guid HouseholdId, int RecipeType = 1, bool IsImported = false, int? DifficultyLevel = null, int Origin = 1);

public sealed record SetRecipeDifficultyRequest(int? DifficultyLevel);

public sealed record SetRecipeTypeRequest(int RecipeType);

public sealed record SetRecipeOriginRequest(int Origin);

public sealed record SetMealsPerCookRequest(int MealsPerCook);

public sealed record ImportRecipeRequest(string Text);
public sealed record ImportRecipeFromUrlRequest(string SourceUrl);

public sealed record UpdateRecipeRequest(string Name);

public sealed record SuggestIngredientSubstitutionsRequest(string IngredientName, string? RecipeContext, string? DietaryGoal);

public sealed record AddRecipeVariationRequest(string Name, string? Notes, string? IngredientAdjustmentNotes);

public sealed record UpdateRecipeVariationOverridesRequest(IReadOnlyList<RecipeVariationIngredientOverrideRequest> Overrides);

public sealed record RecipeVariationIngredientOverrideRequest(string IngredientName, decimal? Quantity, string? Unit, bool IsRemoved);

public sealed record ScaleRecipeRequest(int FromServings, int ToServings);

public sealed record BatchAnalyzeRequest(IReadOnlyList<Guid> RecipeIds);

public sealed record UpdateIngredientInRecipeRequest(string Name, decimal Quantity, string Unit);

public sealed record RateRecipeRequest(int Stars, string? Comment);