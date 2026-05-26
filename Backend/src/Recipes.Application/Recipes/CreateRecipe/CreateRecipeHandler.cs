using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed class CreateRecipeHandler : IRequestHandler<CreateRecipeCommand, ErrorOr<CreateRecipeResponse>>
{
    private readonly IRecipeRepository _repository;
    private readonly ICurrentUser _currentUser;

    public CreateRecipeHandler(IRecipeRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<CreateRecipeResponse>> Handle(
        CreateRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var householdId = HouseholdId.From(request.HouseholdId);
        var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);

        if (!memberIds.Contains(householdId))
        {
            return Error.Forbidden("Recipe.HouseholdAccessDenied", "You are not a member of the specified household.");
        }

        var recipe = new Recipe(request.Name, householdId);
        recipe.SetRecipeType((RecipeType)request.RecipeType);
        if (request.IsImported) recipe.MarkAsImported();

        _repository.Add(recipe);
        await _repository.SaveChangesAsync(cancellationToken);

        return new CreateRecipeResponse(recipe.Id.Value);
    }
}
