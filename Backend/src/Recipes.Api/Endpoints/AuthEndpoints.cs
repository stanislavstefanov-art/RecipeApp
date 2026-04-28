using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Auth.Login;
using Recipes.Application.Auth.Me;
using Recipes.Application.Auth.Register;

namespace Recipes.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterCommand(request.Email, request.Password, request.DisplayName), ct);
            return result.ToHttpResult(dto => Results.Created("/api/auth/me", dto));
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        }).AllowAnonymous();

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new MeQuery(), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        }).RequireAuthorization();

        return app;
    }

    private sealed record RegisterRequest(string Email, string Password, string DisplayName);
    private sealed record LoginRequest(string Email, string Password);
}
