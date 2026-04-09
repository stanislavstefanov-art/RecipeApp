namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class RecipeStep
{
    public RecipeStepId Id { get; private set; } = RecipeStepId.New();
    public RecipeId RecipeId { get; private set; }
    public int Order { get; private set; }
    public string Instruction { get; private set; } = string.Empty;

    private RecipeStep() { }

    internal RecipeStep(RecipeId recipeId, int order, string instruction)
    {
        RecipeId = recipeId;
        Order = order;
        Instruction = instruction;
    }
}

