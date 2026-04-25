namespace Recipes.McpServer.Http;

public sealed record RecipeListItemDto(Guid Id, string Name);

public sealed record RecipeDto(
    Guid Id,
    string Name,
    IReadOnlyList<IngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps);

public sealed record IngredientDto(string Name, decimal Quantity, string Unit);

public sealed record RecipeStepDto(int Order, string Instruction);

public sealed record MealPlanListItemDto(
    Guid Id,
    string Name,
    Guid HouseholdId,
    string HouseholdName,
    int EntryCount);

public sealed record MealPlanDetailsDto(
    Guid Id,
    string Name,
    Guid HouseholdId,
    string HouseholdName,
    IReadOnlyList<MealPlanEntryDto> Entries);

public sealed record MealPlanEntryDto(
    Guid Id,
    Guid BaseRecipeId,
    string BaseRecipeName,
    DateOnly PlannedDate,
    int MealType,
    int Scope,
    IReadOnlyList<MealPlanEntryAssignmentDto> Assignments);

public sealed record MealPlanEntryAssignmentDto(
    Guid PersonId,
    string PersonName,
    Guid AssignedRecipeId,
    string AssignedRecipeName,
    Guid? RecipeVariationId,
    string? RecipeVariationName,
    decimal PortionMultiplier,
    string? Notes);

public sealed record ShoppingListSummaryDto(
    Guid Id,
    string Name,
    IReadOnlyList<ShoppingListItemDto> Items);

public sealed record ShoppingListDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<ShoppingListItemDto> Items);

public sealed record ShoppingListItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    string Unit,
    bool IsPurchased,
    string? Notes,
    int SourceType,
    Guid? SourceReferenceId);

public sealed record MonthlyExpenseReportDto(
    int Year,
    int Month,
    decimal TotalAmount,
    string Currency,
    int ExpenseCount,
    decimal AverageExpenseAmount,
    string? TopCategory,
    decimal FoodPercentage,
    MonthlyExpenseLargestItemDto? LargestExpense,
    IReadOnlyList<MonthlyExpenseCategoryBreakdownDto> Categories);

public sealed record MonthlyExpenseCategoryBreakdownDto(
    string Category,
    decimal Amount,
    decimal Percentage);

public sealed record MonthlyExpenseLargestItemDto(
    decimal Amount,
    string Description,
    DateOnly ExpenseDate,
    string Category);

public sealed record HouseholdSummaryDto(Guid Id, string Name, int MemberCount);

public sealed record HouseholdDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<HouseholdMemberDto> Members);

public sealed record HouseholdMemberDto(
    Guid PersonId,
    string PersonName,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes);
