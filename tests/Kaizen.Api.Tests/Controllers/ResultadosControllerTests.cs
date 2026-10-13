using Kaizen.Api.Tests.Infra;
using Xunit;

namespace Kaizen.Api.Tests.Controllers;

public class ResultadosControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public ResultadosControllerTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Get_ReturnsHttpStatusCode200()
    {
        // Arrange
        var request = "/api/resultados?page=1&pageSize=10";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode(); // verifica status 2xx (200)
    }
}
