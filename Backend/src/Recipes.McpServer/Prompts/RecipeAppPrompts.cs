using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Recipes.McpServer.Prompts;

[McpServerPromptType]
public sealed class RecipeAppPrompts
{
    [McpServerPrompt(Name = "plan_week_for_household")]
    [Description("Suggest a 7-day meal plan for a household.")]
    public IEnumerable<PromptMessage> PlanWeekForHousehold(
        [Description("Household ID (GUID).")] string householdId,
        [Description("Start date in YYYY-MM-DD format.")] string startDate)
    {
        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"""
                    Please create a 7-day meal plan for household {householdId} starting {startDate}.

                    Steps:
                    1. Use `get_household` to look up the members and their dietary preferences.
                    2. Use `list_recipes` (and `get_recipe` for details) to explore available recipes.
                    3. Suggest a balanced 7-day plan (breakfast, lunch, dinner) that fits the members' preferences.
                    4. Avoid repeating the same recipe more than twice across the week.
                    5. Present the plan as a table: Date | Meal | Recipe | Notes.
                    """
            }
        };
    }

    [McpServerPrompt(Name = "weekly_budget_review")]
    [Description("Analyse monthly expenses and suggest budget improvements.")]
    public IEnumerable<PromptMessage> WeeklyBudgetReview(
        [Description("Year (e.g. 2026).")] string year,
        [Description("Month number (1–12).")] string month)
    {
        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"""
                    Please review the household budget for {year}-{int.Parse(month):D2}.

                    Steps:
                    1. Call `get_monthly_expense_report` with year={year} and month={month}.
                    2. Summarise total spend, top spending categories, and the largest single expense.
                    3. Compare food spending to overall spending (foodPercentage field).
                    4. Suggest 2–3 concrete ways to reduce costs next month.
                    """
            }
        };
    }

    [McpServerPrompt(Name = "substitute_ingredient_for_recipe")]
    [Description("Suggest substitutes for a named ingredient in a recipe.")]
    public IEnumerable<PromptMessage> SubstituteIngredientForRecipe(
        [Description("Recipe ID (GUID).")] string recipeId,
        [Description("Name of the ingredient to substitute.")] string ingredientName)
    {
        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"""
                    Please suggest substitutes for '{ingredientName}' in recipe {recipeId}.

                    Steps:
                    1. Call `get_recipe` with id={recipeId} to retrieve the full recipe.
                    2. Identify the role of '{ingredientName}' in the dish (flavour, texture, binding, etc.).
                    3. Suggest three substitutes, each with:
                       - The substitute ingredient and any quantity adjustment.
                       - How it changes the flavour or texture.
                       - Any dietary benefit (vegan, gluten-free, etc.) if relevant.
                    """
            }
        };
    }
}
