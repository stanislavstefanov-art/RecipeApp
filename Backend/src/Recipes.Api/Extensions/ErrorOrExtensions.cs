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
            ErrorType.NotFound     => Results.Problem(
                detail: first.Description, statusCode: StatusCodes.Status404NotFound,
                extensions: new Dictionary<string, object?> { ["code"] = first.Code }),
            ErrorType.Conflict     => Results.Problem(
                detail: first.Description, statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["code"] = first.Code }),
            ErrorType.Unauthorized => Results.Problem(
                detail: first.Description, statusCode: StatusCodes.Status401Unauthorized,
                extensions: new Dictionary<string, object?> { ["code"] = first.Code }),
            ErrorType.Forbidden    => Results.Problem(
                detail: first.Description, statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["code"] = first.Code }),
            ErrorType.Validation   => Results.ValidationProblem(
                new Dictionary<string, string[]> { [first.Code] = [first.Description] }),
            _                      => Results.Problem(
                detail: first.Description, statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["code"] = first.Code }),
        };
    }
}
