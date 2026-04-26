using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Recipes.Api.Tests.Infrastructure;
using Recipes.Application.Recipes.CreateRecipe;

namespace Recipes.Api.Tests.Recipes;

[Trait("Category", "Docker")]
public sealed class RecipesEndpointsTests(IntegrationTestWebApplicationFactory factory)
    : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateRecipe_WithValidName_Returns201WithLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/recipes", new { Name = "Pasta" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateRecipe_WithEmptyName_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/recipes", new { Name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Name");
    }

    [Fact]
    public async Task GetRecipe_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/recipes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRecipe_ThenGet_ReturnsSameRecipe()
    {
        var create = await _client.PostAsJsonAsync("/api/recipes", new { Name = "Risotto" });
        var created = await create.Content.ReadFromJsonAsync<CreateRecipeResponse>();

        var get = await _client.GetAsync($"/api/recipes/{created!.Id}");

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await get.Content.ReadAsStringAsync();
        body.Should().Contain("Risotto");
    }

    [Fact]
    public async Task ListRecipes_ReturnsOkWithArray()
    {
        var response = await _client.GetAsync("/api/recipes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ImportRecipe_WithValidText_ReturnsOkWithStubPayload()
    {
        var response = await _client.PostAsJsonAsync("/api/recipes/import", new
        {
            Text = "2 eggs, 1 tomato, fry for 5 minutes"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Imported recipe");
        body.Should().Contain("Eggs");
    }

    [Fact]
    public async Task ImportRecipe_WithEmptyText_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/recipes/import", new
        {
            Text = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
