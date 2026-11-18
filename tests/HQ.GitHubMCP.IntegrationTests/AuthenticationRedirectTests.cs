using System.Net;
using Xunit;

namespace HQ.GitHubMCP.IntegrationTests;

public sealed class AuthenticationRedirectTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticationRedirectTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Home_WhenAnonymous_ChallengesWithOpenIdConnectRedirect()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("login.microsoftonline.com", response.Headers.Location!.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task McpMetadata_WhenAnonymous_DoesNotRedirectLoopEndpoint()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
