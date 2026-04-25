using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ReviewRecipeDraft;

public sealed record RecipeDraftReviewDto(
    ImportedRecipeDto Draft,
    string Consensus,
    IReadOnlyList<JurorVerdictDto> JurorVerdicts);

public sealed record JurorVerdictDto(
    int JurorIndex,
    string Role,
    string Verdict,
    string Confidence,
    IReadOnlyList<string> Issues);
