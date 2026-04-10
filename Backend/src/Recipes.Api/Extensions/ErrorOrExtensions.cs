using ErrorOr;

namespace Recipes.Api.Extensions;

public static class ErrorOrExtensions
{
    public static IResult ToHttpResult<T>(this ErrorOr<T> errorOr, Func<T, IResult> onValue)
        => errorOr.Match(onValue, errors => errors.ToHttpResult());

    private static IResult ToHttpResult(this List<Error> errors)
    {
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var dict = errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return Results.ValidationProblem(dict);
        }

        var first = errors[0];
        return first.Type switch
        {
            ErrorType.NotFound   => Results.NotFound(),
            ErrorType.Conflict   => Results.Conflict(),
            ErrorType.Validation => Results.ValidationProblem(
                new Dictionary<string, string[]> { [first.Code] = [first.Description] }),
            _                    => Results.Problem(first.Description)
        };
    }
}
