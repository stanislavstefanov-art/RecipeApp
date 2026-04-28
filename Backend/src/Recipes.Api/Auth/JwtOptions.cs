namespace Recipes.Api.Auth;

public sealed class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "RecipesApp";
    public string Audience { get; set; } = "RecipesApp";
    public int LifetimeDays { get; set; } = 7;
}
