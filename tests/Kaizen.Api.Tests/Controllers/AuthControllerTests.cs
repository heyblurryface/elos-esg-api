using System.Net.Http.Json;
using Kaizen.Api.Tests.Infra;
using Xunit;

namespace Kaizen.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public AuthControllerTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Login_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/auth/login";
        var body = new { email = "admin@kaizen.com.br", senha = "kaizen123" };

        // Act
        var response = await _client.PostAsJsonAsync(request, body);

        // Assert
        response.EnsureSuccessStatusCode(); // verifica status 2xx (200)
    }
}
