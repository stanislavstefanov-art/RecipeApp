using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class NutritionTools
{
    [McpServerTool(Name = "lookup_nutrition")]
    [Description("Look up nutritional information for an ingredient. Returns calories, protein, carbs, fat, and fiber per the given quantity and unit.")]
    public string LookupNutrition(
        [Description("Ingredient name (e.g. 'spaghetti', 'olive oil').")] string ingredient,
        [Description("Numeric amount.")] double quantity,
        [Description("Unit of measure (g, ml, tbsp, cup, etc.).")] string unit)
    {
        // Deterministic stub: hash the ingredient name to produce consistent values
        var hash = (uint)ingredient.ToLowerInvariant().GetHashCode();
        var calorieBase  = (hash % 40 + 5)  * 1.5;   // 7.5–67.5 kcal/100 units
        var proteinBase  = (hash % 25 + 1)  * 0.04;  // 0.04–1.0 g/unit
        var carbBase     = (hash % 40 + 2)  * 0.06;  // 0.12–2.52 g/unit
        var fatBase      = (hash % 20 + 1)  * 0.02;  // 0.02–0.42 g/unit
        var fiberBase    = (hash % 12 + 1)  * 0.01;  // 0.01–0.12 g/unit

        var result = new
        {
            ingredient,
            quantity,
            unit,
            calories = Math.Round(calorieBase * quantity / 100.0, 1),
            proteinG = Math.Round(proteinBase * quantity / 100.0, 1),
            carbG    = Math.Round(carbBase    * quantity / 100.0, 1),
            fatG     = Math.Round(fatBase     * quantity / 100.0, 1),
            fiberG   = Math.Round(fiberBase   * quantity / 100.0, 1),
        };
        return JsonSerializer.Serialize(result);
    }
}
