using ErrorOr;
using MediatR;
using Recipes.Domain.Entities;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed class CreateRecipeHandler : IRequestHandler<CreateRecipeCommand, ErrorOr<CreateRecipeResponse>>
{
    private readonly IRecipeRepository _repository;

    public CreateRecipeHandler(IRecipeRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<CreateRecipeResponse>> Handle(
        CreateRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipe = new Recipe(request.Name);

        _repository.Add(recipe);
        await _repository.SaveChangesAsync(cancellationToken);

        return new CreateRecipeResponse(recipe.Id.Value);
    }
}
