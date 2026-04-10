using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Application.Behaviors;
using Recipes.Application.Recipes.CreateRecipe;

namespace Recipes.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateRecipeCommand>();

        return services;
    }
}
