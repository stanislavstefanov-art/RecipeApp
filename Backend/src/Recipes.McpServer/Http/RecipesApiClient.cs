using System.Net;
using System.Net.Http.Json;

namespace Recipes.McpServer.Http;

public sealed class RecipesApiClient
{
    private readonly HttpClient _http;

    public RecipesApiClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<RecipeListItemDto>> ListRecipesAsync(CancellationToken ct)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<RecipeListItemDto>>("/api/recipes", ct);
        return result ?? [];
    }

    public async Task<RecipeDto?> GetRecipeAsync(string id, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/recipes/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeDto>(ct);
    }

    public async Task<IReadOnlyList<RecipeListItemDto>> SearchRecipesByIngredientAsync(string ingredient, CancellationToken ct)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<RecipeListItemDto>>(
            $"/api/recipes/search?ingredient={Uri.EscapeDataString(ingredient)}", ct);
        return result ?? [];
    }

    public async Task<IReadOnlyList<MealPlanListItemDto>> ListMealPlansAsync(CancellationToken ct)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<MealPlanListItemDto>>("/api/meal-plans", ct);
        return result ?? [];
    }

    public async Task<MealPlanDetailsDto?> GetMealPlanAsync(string id, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/meal-plans/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanDetailsDto>(ct);
    }

    public async Task<IReadOnlyList<ShoppingListSummaryDto>> ListShoppingListsAsync(CancellationToken ct)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<ShoppingListSummaryDto>>("/api/shopping-lists", ct);
        return result ?? [];
    }

    public async Task<ShoppingListDetailsDto?> GetShoppingListAsync(string id, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/shopping-lists/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ShoppingListDetailsDto>(ct);
    }

    public async Task MarkItemPendingAsync(string shoppingListId, string itemId, CancellationToken ct)
    {
        var response = await _http.PostAsync(
            $"/api/shopping-lists/{shoppingListId}/items/{itemId}/pending", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkItemPurchasedAsync(string shoppingListId, string itemId, CancellationToken ct)
    {
        var response = await _http.PostAsync(
            $"/api/shopping-lists/{shoppingListId}/items/{itemId}/purchase", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<MonthlyExpenseReportDto?> GetMonthlyExpenseReportAsync(int year, int month, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/expenses/monthly-report?year={year}&month={month}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MonthlyExpenseReportDto>(ct);
    }

    public async Task<IReadOnlyList<HouseholdSummaryDto>> ListHouseholdsAsync(CancellationToken ct)
    {
        var result = await _http.GetFromJsonAsync<IReadOnlyList<HouseholdSummaryDto>>("/api/households", ct);
        return result ?? [];
    }

    public async Task<HouseholdDetailsDto?> GetHouseholdAsync(string id, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/api/households/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HouseholdDetailsDto>(ct);
    }
}
