using Recipes.Api.Endpoints;
using Recipes.Application;
using Recipes.Application.Behaviors;
using Recipes.Infrastructure;

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
