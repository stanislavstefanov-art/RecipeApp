using FluentValidation;

namespace Recipes.Application.Recipes.MoveStep;

public sealed class MoveStepValidator : AbstractValidator<MoveStepCommand>
{
    public MoveStepValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Direction).NotEmpty().Must(d => d == "up" || d == "down")
            .WithMessage("Direction must be 'up' or 'down'.");
    }
}
