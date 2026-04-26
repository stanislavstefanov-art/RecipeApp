using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Application.Recipes.ReviewRecipeDraft;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubRecipeDraftReviewService : IRecipeDraftReviewService
{
    public Task<RecipeDraftReviewDto> ReviewAsync(ImportedRecipeDto draft, CancellationToken cancellationToken)
    {
        var verdicts = new List<JurorVerdictDto>
        {
            new(
                JurorIndex: 1,
                Role: "chef",
                Verdict: "approve-with-revisions",
                Confidence: "0.7",
                Issues: new[]
                {
                    "Quantity for the primary fat is missing — recommend 2 tbsp olive oil.",
                    "Step 3 should include heat level (medium-high)."
                }),
            new(
                JurorIndex: 2,
                Role: "home-cook",
                Verdict: "approve",
                Confidence: "0.65",
                Issues: new[]
                {
                    "Looks doable on a weeknight; no equipment beyond a single pan."
                }),
            new(
                JurorIndex: 3,
                Role: "nutritionist",
                Verdict: "approve-with-revisions",
                Confidence: "0.6",
                Issues: new[]
                {
                    "No portion size declared — recommend listing servings.",
                    "Sodium guidance is implicit; suggest a measured starting amount."
                })
        };

        var result = new RecipeDraftReviewDto(
            Draft: draft,
            Consensus: "approve-with-revisions",
            JurorVerdicts: verdicts,
            ProvenanceId: Guid.Empty);

        return Task.FromResult(result);
    }
}
