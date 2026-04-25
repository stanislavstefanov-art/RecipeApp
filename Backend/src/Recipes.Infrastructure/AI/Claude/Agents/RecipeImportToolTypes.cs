using System.Text.Json.Serialization;

namespace Recipes.Infrastructure.AI.Claude.Agents;

internal sealed record FetchUrlInput(
    [property: JsonPropertyName("url")] string Url
);

internal sealed record ExtractRecipeFieldsInput(
    [property: JsonPropertyName("title")]       string?                 Title,
    [property: JsonPropertyName("servings")]    int?                    Servings,
    [property: JsonPropertyName("ingredients")] List<RawIngredientItem> Ingredients,
    [property: JsonPropertyName("steps")]       List<string>            Steps,
    [property: JsonPropertyName("notes")]       string?                 Notes
);

internal sealed record RawIngredientItem(
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("rawQuantity")] string? RawQuantity,
    [property: JsonPropertyName("rawUnit")]     string? RawUnit,
    [property: JsonPropertyName("notes")]       string? Notes
);

internal sealed record NormalizeIngredientInput(
    [property: JsonPropertyName("name")]        string  Name,
    [property: JsonPropertyName("rawQuantity")] string? RawQuantity,
    [property: JsonPropertyName("rawUnit")]     string? RawUnit
);

internal sealed record NormalizeIngredientResult(
    [property: JsonPropertyName("normalizedName")] string   NormalizedName,
    [property: JsonPropertyName("quantity")]        decimal? Quantity,
    [property: JsonPropertyName("unit")]            string?  Unit
);

internal sealed record SaveRecipeInput(
    [property: JsonPropertyName("title")]       string?                        Title,
    [property: JsonPropertyName("servings")]    int?                           Servings,
    [property: JsonPropertyName("ingredients")] List<NormalizedIngredientItem> Ingredients,
    [property: JsonPropertyName("steps")]       List<string>                   Steps,
    [property: JsonPropertyName("notes")]       string?                        Notes,
    [property: JsonPropertyName("confidence")]  double                         Confidence,
    [property: JsonPropertyName("needsReview")] bool                           NeedsReview
);

internal sealed record NormalizedIngredientItem(
    [property: JsonPropertyName("name")]     string   Name,
    [property: JsonPropertyName("quantity")] decimal? Quantity,
    [property: JsonPropertyName("unit")]     string?  Unit,
    [property: JsonPropertyName("notes")]    string?  Notes
);
