using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MemoriaDigital.IntegrationTests;

public sealed class AnalysisApiTests
{
    [Fact]
    public async Task PostAnalysis_ReturnsCreated()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/analyses", ValidMessage());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreatedAnalysis_CanBeQueriedLater()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var created = await CreateAnalysisAsync(client);

        // Act
        using var response = await client.GetAsync($"/api/analyses/{created.Id}");
        var analysis = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id.ToString(), analysis.GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetById_ExistingAnalysis_ReturnsOk()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var created = await CreateAnalysisAsync(client);

        // Act
        using var response = await client.GetAsync($"/api/analyses/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_MissingAnalysis_ReturnsNotFound()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/analyses/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_ReturnsItemsAndPaginationMetadata()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await CreateAnalysisAsync(client);
        await CreateAnalysisAsync(client);
        await CreateAnalysisAsync(client);

        // Act
        using var response = await client.GetAsync("/api/analyses?page=1&pageSize=2");
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.Equal(1, page.GetProperty("pageNumber").GetInt32());
        Assert.Equal(2, page.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, page.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task GetPaged_OrdersByCreatedAtDescendingAndIdAsTieBreaker()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        await CreateAnalysisAsync(client);
        await CreateAnalysisAsync(client);
        await CreateAnalysisAsync(client);

        // Act
        using var response = await client.GetAsync("/api/analyses?page=1&pageSize=10");
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = page.GetProperty("items").EnumerateArray().ToArray();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        for (var index = 1; index < items.Length; index++)
        {
            var previousCreatedAt = items[index - 1].GetProperty("createdAt").GetDateTimeOffset();
            var currentCreatedAt = items[index].GetProperty("createdAt").GetDateTimeOffset();

            Assert.True(previousCreatedAt >= currentCreatedAt);

            if (previousCreatedAt == currentCreatedAt)
            {
                var previousId = items[index - 1].GetProperty("id").GetString();
                var currentId = items[index].GetProperty("id").GetString();
                Assert.True(string.CompareOrdinal(previousId, currentId) >= 0);
            }
        }
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetPaged_InvalidParameters_ReturnsBadRequest(int page, int pageSize)
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync($"/api/analyses?page={page}&pageSize={pageSize}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAnalysis_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var invalidRequest = new
        {
            inputType = "Message",
            content = ""
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/analyses", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalysisEnums_AreSerializedAsStrings()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync("/api/analyses", ValidMessage());
        var analysis = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal("Completed", analysis.GetProperty("status").GetString());
        Assert.Equal("Message", analysis.GetProperty("input").GetProperty("type").GetString());
    }

    [Fact]
    public async Task PostSuspiciousLink_PersistsCompletedRiskResult()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var request = new
        {
            inputType = "Link",
            content = "https://192.168.0.1/login"
        };

        // Act
        using var createResponse = await client.PostAsJsonAsync("/api/analyses", request);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetGuid();
        using var getResponse = await client.GetAsync($"/api/analyses/{id}");
        var persisted = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal("Completed", persisted.GetProperty("status").GetString());
        Assert.Equal("Medium", persisted.GetProperty("riskLevel").GetString());
        Assert.Contains(
            persisted.GetProperty("indicators").EnumerateArray(),
            indicator => indicator.GetProperty("code").GetString() == "LINK_IP_ADDRESS");
    }

    [Fact]
    public async Task PostImage_PersistsUnsupportedResult()
    {
        // Arrange
        using var factory = await CreateFactoryAsync();
        using var client = factory.CreateClient();
        var request = new
        {
            inputType = "Image",
            reference = "uploads/image.png"
        };

        // Act
        using var response = await client.PostAsJsonAsync("/api/analyses", request);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Unsupported", result.GetProperty("status").GetString());
        Assert.True(result.GetProperty("riskLevel").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task EachFactory_UsesAnIsolatedDatabase()
    {
        // Arrange
        using var firstFactory = await CreateFactoryAsync();
        using var firstClient = firstFactory.CreateClient();
        await CreateAnalysisAsync(firstClient);

        using var secondFactory = await CreateFactoryAsync();
        using var secondClient = secondFactory.CreateClient();

        // Act
        using var response = await secondClient.GetAsync("/api/analyses?page=1&pageSize=10");
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
    }

    private static async Task<ApiFactory> CreateFactoryAsync()
    {
        var factory = new ApiFactory();
        await factory.InitializeDatabaseAsync();
        return factory;
    }

    private static async Task<CreatedAnalysis> CreateAnalysisAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/analyses", ValidMessage());
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new CreatedAnalysis(Guid.Parse(body.GetProperty("id").GetString()!));
    }

    private static object ValidMessage() => new
    {
        inputType = "Message",
        content = "Mensagem válida para o teste de integração."
    };

    private sealed record CreatedAnalysis(Guid Id);
}
