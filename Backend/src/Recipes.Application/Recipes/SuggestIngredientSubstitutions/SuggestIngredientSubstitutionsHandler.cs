using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.SuggestIngredientSubstitutions;

public sealed class SuggestIngredientSubstitutionsHandler
    : IRequestHandler<SuggestIngredientSubstitutionsCommand, ErrorOr<IngredientSubstitutionSuggestionDto>>
{
    private readonly IIngredientSubstitutionSuggestionService _service;

    public SuggestIngredientSubstitutionsHandler(IIngredientSubstitutionSuggestionService service)
    {
        _service = service;
    }

    public async Task<ErrorOr<IngredientSubstitutionSuggestionDto>> Handle(
        SuggestIngredientSubstitutionsCommand request,
        CancellationToken cancellationToken)
    {
        var dto = await _service.SuggestAsync(
            new IngredientSubstitutionRequestDto(
                request.IngredientName,
                request.RecipeContext,
                request.DietaryGoal),
            cancellationToken);

        return dto;
    }
}