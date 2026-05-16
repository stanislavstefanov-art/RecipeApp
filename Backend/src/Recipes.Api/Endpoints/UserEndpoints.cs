using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Users.ClearUserData;

namespace Recipes.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user")
            .WithTags("User")
            .RequireAuthorization();

        group.MapDelete("/data", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ClearUserDataCommand(), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}
