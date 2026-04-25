namespace Recipes.Application.Recipes.AnalyseRecipeNutrition;

public sealed record NutritionAnalysisDto(
    Guid RecipeId,
    string RecipeName,
    IReadOnlyList<IngredientNutritionDto> Ingredients,
    NutritionTotalsDto Totals);

public sealed record IngredientNutritionDto(
    string Name,
    decimal Quantity,
    string Unit,
    double Calories,
    double ProteinG,
    double CarbG,
    double FatG,
    double FiberG);

public sealed record NutritionTotalsDto(
    double Calories,
    double ProteinG,
    double CarbG,
    double FatG,
    double FiberG);
