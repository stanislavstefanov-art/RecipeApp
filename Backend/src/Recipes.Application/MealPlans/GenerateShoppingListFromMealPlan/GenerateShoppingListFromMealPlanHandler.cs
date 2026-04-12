using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;

public sealed class GenerateShoppingListFromMealPlanHandler
    : IRequestHandler<GenerateShoppingListFromMealPlanCommand, ErrorOr<Success>>
{
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IRecipeRepository _recipeRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPersonRepository _personRepository;

    public GenerateShoppingListFromMealPlanHandler(
        IMealPlanRepository mealPlanRepository,
        IShoppingListRepository shoppingListRepository,
        IRecipeRepository recipeRepository,
        IProductRepository productRepository,
        IPersonRepository personRepository)
    {
        _mealPlanRepository = mealPlanRepository;
        _shoppingListRepository = shoppingListRepository;
        _recipeRepository = recipeRepository;
        _productRepository = productRepository;
        _personRepository = personRepository;
    }

    public async Task<ErrorOr<Success>> Handle(
        GenerateShoppingListFromMealPlanCommand request,
        CancellationToken cancellationToken)
    {
        var mealPlanId = MealPlanId.From(request.MealPlanId);
        var shoppingListId = ShoppingListId.From(request.ShoppingListId);

        var mealPlan = await _mealPlanRepository.GetByIdAsync(mealPlanId, cancellationToken);
        if (mealPlan is null)
        {
            return Error.NotFound(
                "MealPlan.NotFound",
                $"Meal plan '{request.MealPlanId}' was not found.");
        }

        var shoppingList = await _shoppingListRepository.GetByIdAsync(shoppingListId, cancellationToken);
        if (shoppingList is null)
        {
            return Error.NotFound(
                "ShoppingList.NotFound",
                $"Shopping list '{request.ShoppingListId}' was not found.");
        }

        var persons = await _personRepository.GetAllAsync(cancellationToken);
        var personsById = persons.ToDictionary(x => x.Id, x => x.Name);

        foreach (var entry in mealPlan.Entries.OrderBy(x => x.PlannedDate).ThenBy(x => x.MealType))
        {
            foreach (var assignment in entry.PersonAssignments)
            {
                var recipe = await _recipeRepository.GetByIdAsync(assignment.AssignedRecipeId, cancellationToken);
                if (recipe is null)
                {
                    return Error.NotFound(
                        "Recipe.NotFound",
                        $"Recipe '{assignment.AssignedRecipeId.Value}' was not found.");
                }

                var personName = personsById.TryGetValue(assignment.PersonId, out var name)
                    ? name
                    : assignment.PersonId.Value.ToString();

                var variation = assignment.RecipeVariationId.HasValue
                    ? recipe.Variations.SingleOrDefault(x => x.Id == assignment.RecipeVariationId.Value)
                    : null;

                var effectiveIngredients = recipe.Ingredients
                    .Select(i => new EffectiveIngredient(
                        i.Name,
                        i.Quantity,
                        i.Unit))
                    .ToList();

                if (variation is not null)
                {
                    foreach (var ov in variation.IngredientOverrides)
                    {
                        var existing = effectiveIngredients.SingleOrDefault(x =>
                            string.Equals(x.Name, ov.IngredientName, StringComparison.OrdinalIgnoreCase));

                        if (ov.IsRemoved)
                        {
                            if (existing is not null)
                            {
                                effectiveIngredients.Remove(existing);
                            }

                            continue;
                        }

                        if (existing is not null)
                        {
                            if (ov.Quantity.HasValue && !string.IsNullOrWhiteSpace(ov.Unit))
                            {
                                existing.Quantity = ov.Quantity.Value;
                                existing.Unit = ov.Unit!;
                            }
                        }
                        else if (ov.Quantity.HasValue && !string.IsNullOrWhiteSpace(ov.Unit))
                        {
                            effectiveIngredients.Add(new EffectiveIngredient(
                                ov.IngredientName,
                                ov.Quantity.Value,
                                ov.Unit!));
                        }
                    }
                }

                foreach (var ingredient in effectiveIngredients)
                {
                    var product = await _productRepository.GetByNameAsync(ingredient.Name, cancellationToken);

                    if (product is null)
                    {
                        product = new Product(ingredient.Name);
                        await _productRepository.AddAsync(product, cancellationToken);
                    }

                    var scaledQuantity = decimal.Round(
                        ingredient.Quantity * assignment.PortionMultiplier,
                        2,
                        MidpointRounding.AwayFromZero);

                    var notes = BuildNotes(
                        personName,
                        variation?.Name,
                        variation?.IngredientAdjustmentNotes,
                        assignment.Notes);

                    shoppingList.AddItem(
                        product,
                        scaledQuantity,
                        ingredient.Unit,
                        notes,
                        ShoppingListItemSourceType.MealPlan,
                        mealPlan.Id.Value);
                }
            }
        }

        await _shoppingListRepository.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }

    private static string? BuildNotes(
        string personName,
        string? variationName,
        string? variationAdjustmentNotes,
        string? assignmentNotes)
    {
        var parts = new List<string> { $"For {personName}" };

        if (!string.IsNullOrWhiteSpace(variationName))
        {
            parts.Add($"Variation: {variationName}");
        }

        if (!string.IsNullOrWhiteSpace(variationAdjustmentNotes))
        {
            parts.Add($"Adjustment: {variationAdjustmentNotes}");
        }

        if (!string.IsNullOrWhiteSpace(assignmentNotes))
        {
            parts.Add($"Assignment notes: {assignmentNotes}");
        }

        return string.Join(" | ", parts);
    }

    private sealed class EffectiveIngredient
    {
        public EffectiveIngredient(string name, decimal quantity, string unit)
        {
            Name = name;
            Quantity = quantity;
            Unit = unit;
        }

        public string Name { get; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}