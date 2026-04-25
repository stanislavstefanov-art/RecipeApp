using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Application.Abstractions;
using Recipes.Application.Common.AI;
using Recipes.Application.Expenses.GetExpenseInsights;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Application.Recipes.AnalyseRecipeNutrition;
using Recipes.Application.Recipes.ImportRecipeFromUrl;
using Recipes.Application.Recipes.SuggestIngredientSubstitutions;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.AI.Claude.Agents;
using Recipes.Infrastructure.Mcp;
using Recipes.Infrastructure.AI.Claude.Assets;
using Recipes.Infrastructure.AI.Claude.Clients;
using Recipes.Infrastructure.AI.Claude.Services;
using Recipes.Infrastructure.AI.Claude.Services.Stubs;
using Recipes.Infrastructure.Events;
using Recipes.Infrastructure.Options;
using Recipes.Infrastructure.Persistence;
using Recipes.Infrastructure.Services;

namespace Recipes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RecipeImportOptions>(options =>
        {
            configuration.GetSection(RecipeImportOptions.SectionName).Bind(options);
        });
        services.Configure<IngredientSubstitutionOptions>(options =>
        {
            configuration.GetSection(IngredientSubstitutionOptions.SectionName).Bind(options);
        });
        services.Configure<ClaudeOptions>(options =>
        {
            configuration.GetSection(ClaudeOptions.SectionName).Bind(options);
        });
        services.Configure<MealPlanSuggestionOptions>(options =>
        {
            configuration.GetSection(MealPlanSuggestionOptions.SectionName).Bind(options);
        });

        services.AddDbContext<RecipesDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("RecipesDb");
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<IRecipesDbContext>(sp => sp.GetRequiredService<RecipesDbContext>());
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IHouseholdRepository, HouseholdRepository>();

        services.AddScoped<StubRecipeImportService>();
        services.AddScoped<ClaudeRecipeImportService>();
        services.AddScoped<IRecipeImportService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<RecipeImportOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeRecipeImportService>(),
                _ => sp.GetRequiredService<StubRecipeImportService>()
            };
        });

        services.AddScoped<StubMealPlanSuggestionService>();
        services.AddScoped<ClaudeMealPlanSuggestionService>();

        services.AddScoped<IMealPlanSuggestionService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<MealPlanSuggestionOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeMealPlanSuggestionService>(),
                _ => sp.GetRequiredService<StubMealPlanSuggestionService>()
            };
        });

        services.AddScoped<StubIngredientSubstitutionSuggestionService>();
        services.AddScoped<ClaudeIngredientSubstitutionSuggestionService>();

        services.AddScoped<IIngredientSubstitutionSuggestionService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<IngredientSubstitutionOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeIngredientSubstitutionSuggestionService>(),
                _ => sp.GetRequiredService<StubIngredientSubstitutionSuggestionService>()
            };
        });

        services.AddScoped<StubExpenseInsightService>();
        services.AddScoped<ClaudeExpenseInsightService>();

        services.AddScoped<IExpenseInsightService>(_ => _.GetRequiredService<StubExpenseInsightService>());

        services.AddScoped<IClaudeAssetProvider, FileSystemClaudeAssetProvider>();
        services.AddScoped<IRecipeImportOrchestrator, RecipeImportOrchestrator>();
        services.AddScoped<IRecipeImportAgent, RecipeImportAgent>();
        services.AddScoped<IMcpClientHost, NutritionMcpClient>();
        services.AddScoped<INutritionAnalysisAgent, NutritionAnalysisAgent>();

        services.AddHttpClient("ClaudeAgent", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        services.AddHttpClient("RecipeUrlFetcher", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RecipeApp/1.0");
        });

        services.AddHttpClient<IClaudeRecipeImportClient, ClaudeRecipeImportClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient<IClaudeMealPlanSuggestionClient, ClaudeMealPlanSuggestionClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });        
        services.AddHttpClient<IClaudeIngredientSubstitutionClient, ClaudeIngredientSubstitutionClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient<IClaudeExpenseInsightClient, ClaudeExpenseInsightClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}

