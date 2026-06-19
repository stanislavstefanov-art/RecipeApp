using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Users.ClearUserData;
using Recipes.Application.Users.GetUserProfile;

namespace Recipes.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user")
            .WithTags("User")
            .RequireAuthorization();

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserProfileQuery(), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        });

        group.MapDelete("/data", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ClearUserDataCommand(), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}
