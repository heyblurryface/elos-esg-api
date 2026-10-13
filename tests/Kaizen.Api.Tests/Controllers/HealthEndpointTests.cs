using System.Net;
using Kaizen.Api.Tests.Infra;
using Xunit;

namespace Kaizen.Api.Tests.Controllers;

/// <summary>
/// Garante que o endpoint /health (usado pelo Docker e pelo smoke test do
/// pipeline CI/CD) responde 200 e informa o estado do banco.
/// </summary>
public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public HealthEndpointTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Get_Health_ReturnsHttpStatusCode200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Healthy\"", body);
        Assert.Contains("\"database\":\"Up\"", body);
    }
}
