using Microsoft.EntityFrameworkCore;
using Recipes.Api.Endpoints;
using Recipes.Application;
using Recipes.Application.Behaviors;
using Recipes.Infrastructure;
using Recipes.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173", "http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddProblemDetails();

var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
    ?? builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Recipes.Application.Recipes.CreateRecipe.CreateRecipeCommand>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Configuration.GetValue("Seed:Enabled", false))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipesDbContext>();
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }
        if (!await db.Recipes.AnyAsync())
        {
            var seeder = new DemoDataSeeder(db);
            await seeder.SeedAsync(default);
        }
    }
}

app.UseCors("Frontend");

app.MapHealthChecks("/health");

app.MapRecipesEndpoints();
app.MapShoppingListsEndpoints();
app.MapProductsEndpoints();
app.MapMealPlansEndpoints();
app.MapExpensesEndpoints();
app.MapPersonsEndpoints();
app.MapHouseholdsEndpoints();
app.MapAdminEndpoints();

app.Run();
