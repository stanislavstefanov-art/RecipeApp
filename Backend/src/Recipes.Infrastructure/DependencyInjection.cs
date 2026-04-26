using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Application.Abstractions;
using Recipes.Application.Common.AI;
using Recipes.Application.Expenses.GetExpenseInsights;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Application.MealPlans.PlanningWorkflow;
using Recipes.Application.Recipes.BatchAnalyseRecipes;
using Recipes.Application.Recipes.ReviewRecipeDraft;
using Recipes.Application.Recipes.CritiqueRecipe;
using Recipes.Application.Recipes.ScaleRecipe;
using Recipes.Application.MealPlans.SuggestMealPlanMultiAgent;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Application.Recipes.AnalyseRecipeNutrition;
using Recipes.Application.Recipes.ImportRecipeFromUrl;
using Recipes.Application.Recipes.SuggestIngredientSubstitutions;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.AI.Claude.Agents;
using Recipes.Infrastructure.AI.Claude.Hooks;
using Recipes.Infrastructure.AI;
using Recipes.Infrastructure.AI.Calibration;
using Recipes.Infrastructure.AI.AiErrors;
using Recipes.Infrastructure.AI.Escalation;
using Recipes.Infrastructure.AI.Provenance;
using Recipes.Infrastructure.AI.Claude.Workflow;
using Recipes.Infrastructure.Mcp;
using Recipes.Infrastructure.Telemetry;
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
        services.Configure<RecipeCritiqueOptions>(options =>
        {
            configuration.GetSection(RecipeCritiqueOptions.SectionName).Bind(options);
        });
        services.Configure<RecipeScalingOptions>(options =>
        {
            configuration.GetSection(RecipeScalingOptions.SectionName).Bind(options);
        });
        services.Configure<RecipeBatchAnalysisOptions>(options =>
        {
            configuration.GetSection(RecipeBatchAnalysisOptions.SectionName).Bind(options);
        });
        services.Configure<RecipeDraftReviewOptions>(options =>
        {
            configuration.GetSection(RecipeDraftReviewOptions.SectionName).Bind(options);
        });
        services.Configure<ExpenseInsightOptions>(options =>
        {
            configuration.GetSection(ExpenseInsightOptions.SectionName).Bind(options);
        });

        var dbProvider = configuration["Database:Provider"] ?? "SqlServer";
        services.AddDbContext<RecipesDbContext>(options =>
        {
            if (string.Equals(dbProvider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("RecipesDb");
            }
            else
            {
                var connectionString = configuration.GetConnectionString("RecipesDb");
                options.UseSqlServer(connectionString);
            }
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

        services.AddScoped<IExpenseInsightService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<ExpenseInsightOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeExpenseInsightService>(),
                _ => sp.GetRequiredService<StubExpenseInsightService>()
            };
        });

        services.AddScoped<StubRecipeCritiqueService>();
        services.AddScoped<ClaudeRecipeCritiqueService>();
        services.AddScoped<IRecipeCritiqueService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<RecipeCritiqueOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeRecipeCritiqueService>(),
                _ => sp.GetRequiredService<StubRecipeCritiqueService>()
            };
        });

        services.AddScoped<StubRecipeScalingService>();
        services.AddScoped<ClaudeRecipeScalingService>();
        services.AddScoped<IRecipeScalingService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<RecipeScalingOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeRecipeScalingService>(),
                _ => sp.GetRequiredService<StubRecipeScalingService>()
            };
        });

        services.AddScoped<StubRecipeBatchAnalysisService>();
        services.AddScoped<ClaudeRecipeBatchAnalysisService>();
        services.AddScoped<IRecipeBatchAnalysisService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<RecipeBatchAnalysisOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeRecipeBatchAnalysisService>(),
                _ => sp.GetRequiredService<StubRecipeBatchAnalysisService>()
            };
        });

        services.AddScoped<StubRecipeDraftReviewService>();
        services.AddScoped<ClaudeRecipeDraftReviewService>();
        services.AddScoped<IRecipeDraftReviewService>(sp =>
        {
            var options = sp.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<RecipeDraftReviewOptions>>().Value;

            return options.Provider switch
            {
                "Claude" => sp.GetRequiredService<ClaudeRecipeDraftReviewService>(),
                _ => sp.GetRequiredService<StubRecipeDraftReviewService>()
            };
        });

        services.AddScoped<IClaudeAssetProvider, FileSystemClaudeAssetProvider>();
        services.AddScoped<IRecipeImportOrchestrator, RecipeImportOrchestrator>();
        services.AddSingleton<IToolCallTelemetry, InMemoryToolCallTelemetry>();
        services.AddScoped<IRecipeImportAgent, RecipeImportAgent>();
        services.AddScoped<IMcpClientHost, NutritionMcpClient>();
        services.AddScoped<INutritionAnalysisAgent, NutritionAnalysisAgent>();
        services.AddScoped<IContextWindowManager, ContextWindowManager>();
        services.AddScoped<RecipeDiscoverySubAgent>();
        services.AddScoped<MealAssignmentSubAgent>();
        services.AddScoped<IMealPlanOrchestratorAgent, MealPlanOrchestratorAgent>();
        services.AddScoped<IAgentHook, LoggingAgentHook>();
        services.AddScoped<IAgentHook, SlotCapacityHook>();
        services.AddScoped<IAgentHookRunner, AgentHookRunner>();
        services.AddSingleton<IConfidenceCalibrationStore, InMemoryConfidenceCalibrationStore>();
        services.AddSingleton<IEscalationStore, InMemoryEscalationStore>();
        services.AddSingleton<IProvenanceStore, InMemoryProvenanceStore>();
        services.AddSingleton<IAiErrorStore, InMemoryAiErrorStore>();
        services.AddSingleton<IWorkflowSessionStore, InMemoryWorkflowSessionStore>();
        services.AddScoped<IWorkflowGate, MinSlotsCoveredGate>();
        services.AddScoped<IWorkflowGate, RecipeDiversityGate>();
        services.AddScoped<IWorkflowGate, MemberCoverageGate>();
        services.AddScoped<IMealPlanWorkflowEnforcer, MealPlanWorkflowEnforcer>();

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

