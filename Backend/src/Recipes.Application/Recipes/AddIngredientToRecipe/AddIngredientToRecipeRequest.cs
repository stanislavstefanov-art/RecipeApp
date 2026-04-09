namespace Recipes.Application.Recipes.AddIngredientToRecipe;

public sealed record AddIngredientToRecipeRequest(string Name, decimal Quantity, string Unit);

