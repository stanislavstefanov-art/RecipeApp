using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Recipes.Infrastructure.AI.Claude.Agents;
using Recipes.Infrastructure.Options;

namespace Recipes.Application.Tests.Recipes.ImportRecipeFromUrl;

public sealed class RecipeImportAgentTests
{
    private const string RecipeUrl = "https://example.com/carbonara";

    // ── Shared Claude response fragments ──────────────────────────────────

    private static string FetchUrlCallResponse(string toolId) => $$"""
        {
          "id": "msg_fetch",
          "stop_reason": "tool_use",
          "content": [{
            "type": "tool_use",
            "id": "{{toolId}}",
            "name": "fetch_url_content",
            "input": { "url": "{{RecipeUrl}}" }
          }],
          "usage": { "input_tokens": 100, "output_tokens": 20 }
        }
        """;

    private static string ExtractAndNormalizeResponse(string extractId, string normId) => $$"""
        {
          "id": "msg_extract",
          "stop_reason": "tool_use",
          "content": [
            {
              "type": "tool_use",
              "id": "{{extractId}}",
              "name": "extract_recipe_fields",
              "input": {
                "title": "Spaghetti Carbonara",
                "servings": 2,
                "ingredients": [{ "name": "Spaghetti", "rawQuantity": "400", "rawUnit": "Grams", "notes": null }],
                "steps": ["Boil pasta", "Mix eggs and cheese"],
                "notes": null
              }
            },
            {
              "type": "tool_use",
              "id": "{{normId}}",
              "name": "normalize_ingredient",
              "input": { "name": "Spaghetti", "rawQuantity": "400", "rawUnit": "Grams" }
            }
          ],
          "usage": { "input_tokens": 300, "output_tokens": 80 }
        }
        """;

    private static string SaveRecipeResponse(string toolId, bool needsReview = false) => $$"""
        {
          "id": "msg_save",
          "stop_reason": "tool_use",
          "content": [{
            "type": "tool_use",
            "id": "{{toolId}}",
            "name": "save_recipe",
            "input": {
              "title": "Spaghetti Carbonara",
              "servings": 2,
              "ingredients": [{ "name": "spaghetti", "quantity": 400, "unit": "g", "notes": null }],
              "steps": ["Boil pasta", "Mix eggs and cheese"],
              "notes": null,
              "confidence": 0.92,
              "needsReview": {{(needsReview ? "true" : "false")}}
            }
          }],
          "usage": { "input_tokens": 400, "output_tokens": 60 }
        }
        """;

    // ── T1: Happy path ─────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_HappyPath_ReturnsPopulatedDto()
    {
        var agent = BuildAgent(
            claudeResponses:
            [
                ClaudeOk(FetchUrlCallResponse("tu1")),
                ClaudeOk(ExtractAndNormalizeResponse("tu2", "tu3")),
                ClaudeOk(SaveRecipeResponse("tu4")),
            ],
            urlFetcherResponse: UrlOk("<html>A lovely recipe page with lots of text here.</html>")
        );

        var result = await agent.RunAsync(RecipeUrl, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("Spaghetti Carbonara");
        result.Value.Servings.Should().Be(2);
        result.Value.Ingredients.Should().HaveCount(1);
        result.Value.Ingredients[0].Name.Should().Be("spaghetti");
        result.Value.Steps.Should().HaveCount(2);
        result.Value.NeedsReview.Should().BeFalse();
        result.Value.Confidence.Should().BeApproximately(0.92, 0.001);
    }

    // ── T2: Fetch failure → retry → save ──────────────────────────────────

    [Fact]
    public async Task RunAsync_FirstFetchFails_RetriesAndSucceeds()
    {
        // Claude calls fetch twice: first time our handler returns non-2xx,
        // second time it returns 200. Claude then proceeds to extract and save.
        var urlFetchCount = 0;
        var urlHandler = new LambdaHttpMessageHandler(_ =>
        {
            urlFetchCount++;
            return urlFetchCount == 1
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : UrlOk("<html>Recipe page content that is long enough to pass the length check.</html>").Content.ReadAsStringAsync().Result is var body
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }
                    : new HttpResponseMessage(HttpStatusCode.OK);
        });

        var agent = BuildAgent(
            claudeResponses:
            [
                ClaudeOk(FetchUrlCallResponse("tu1")),   // fetch → is_error (non-2xx)
                ClaudeOk(FetchUrlCallResponse("tu2")),   // retry fetch → success
                ClaudeOk(ExtractAndNormalizeResponse("tu3", "tu4")),
                ClaudeOk(SaveRecipeResponse("tu5")),
            ],
            urlFetcherHandler: urlHandler
        );

        var result = await agent.RunAsync(RecipeUrl, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Title.Should().Be("Spaghetti Carbonara");
    }

    // ── T3: Two fetch failures → save with needsReview ────────────────────

    [Fact]
    public async Task RunAsync_BothFetchesFail_SavesWithNeedsReview()
    {
        var agent = BuildAgent(
            claudeResponses:
            [
                ClaudeOk(FetchUrlCallResponse("tu1")),    // fetch → is_error
                ClaudeOk(FetchUrlCallResponse("tu2")),    // retry → is_error
                ClaudeOk(SaveRecipeResponse("tu3", needsReview: true)),
            ],
            urlFetcherResponse: new HttpResponseMessage(HttpStatusCode.NotFound)
        );

        var result = await agent.RunAsync(RecipeUrl, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.NeedsReview.Should().BeTrue();
    }

    // ── T4: Loop never calls save_recipe ──────────────────────────────────

    [Fact]
    public async Task RunAsync_MaxIterationsExceeded_ReturnsError()
    {
        // Every Claude response just calls fetch_url_content again, never save_recipe.
        var infiniteFetches = Enumerable.Range(0, 11)
            .Select(i => ClaudeOk(FetchUrlCallResponse($"tu{i}")))
            .ToArray();

        var agent = BuildAgent(
            claudeResponses: infiniteFetches,
            urlFetcherResponse: UrlOk("<html>content long enough to pass the length check here</html>")
        );

        var result = await agent.RunAsync(RecipeUrl, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Description.Should().Contain("maximum iterations");
    }

    // ── T5: end_turn without save_recipe ──────────────────────────────────

    [Fact]
    public async Task RunAsync_ClaudeEndsWithoutSaving_ReturnsError()
    {
        const string endTurnResponse = """
            {
              "id": "msg_1",
              "stop_reason": "end_turn",
              "content": [{ "type": "text", "text": "I cannot extract a recipe from this URL." }],
              "usage": { "input_tokens": 50, "output_tokens": 10 }
            }
            """;

        var agent = BuildAgent(
            claudeResponses: [ClaudeOk(endTurnResponse)],
            urlFetcherResponse: UrlOk("<html>not a recipe page content</html>")
        );

        var result = await agent.RunAsync(RecipeUrl, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Description.Should().Contain("save_recipe");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static RecipeImportAgent BuildAgent(
        HttpResponseMessage[] claudeResponses,
        HttpResponseMessage? urlFetcherResponse = null,
        HttpMessageHandler? urlFetcherHandler = null)
    {
        var claudeHandler = new ScriptedHttpMessageHandler(claudeResponses);
        var fetchHandler  = urlFetcherHandler
                            ?? new LambdaHttpMessageHandler(_ => urlFetcherResponse
                                ?? UrlOk("<html>default recipe content long enough to pass</html>"));

        var factory = new NamedClientFactory(new Dictionary<string, HttpClient>
        {
            ["ClaudeAgent"]      = new HttpClient(claudeHandler),
            ["RecipeUrlFetcher"] = new HttpClient(fetchHandler),
        });

        var options = Options.Create(new ClaudeOptions
        {
            ApiKey    = "test-key",
            Model     = "claude-haiku-4-5",
            MaxTokens = 1000,
        });

        return new RecipeImportAgent(factory, options, NullLogger<RecipeImportAgent>.Instance);
    }

    private static HttpResponseMessage ClaudeOk(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage UrlOk(string html) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(html, Encoding.UTF8, "text/html")
        };
}

// ── Test infrastructure ────────────────────────────────────────────────────

file sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _queue;

    public ScriptedHttpMessageHandler(IEnumerable<HttpResponseMessage> responses)
        => _queue = new Queue<HttpResponseMessage>(responses);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!_queue.TryDequeue(out var response))
            throw new InvalidOperationException("ScriptedHttpMessageHandler: no more scripted responses.");
        return Task.FromResult(response);
    }
}

file sealed class LambdaHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public LambdaHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}

file sealed class NamedClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients;

    public NamedClientFactory(Dictionary<string, HttpClient> clients)
        => _clients = clients;

    public HttpClient CreateClient(string name)
        => _clients.TryGetValue(name, out var c) ? c
           : throw new InvalidOperationException($"No client registered for '{name}'.");
}
