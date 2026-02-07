using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.Endpoints;

public class ModelsEndpointDebugTests : IClassFixture<SynaxisWebApplicationFactory>
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ModelsEndpointDebugTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
    {
        this._factory = factory;
        this._client = factory.CreateClient();
        this._output = output;
    }

    [Fact]
    public async Task Debug_GetModels_ReturnsList()
    {
        var response = await this._client.GetAsync("/openai/v1/models");
        var content = await response.Content.ReadAsStringAsync();

        this._output.WriteLine("Response Status: {0}", response.StatusCode);
        this._output.WriteLine("Response Content: {0}", content);

        response.EnsureSuccessStatusCode();

        var json = JsonSerializer.Deserialize<JsonElement>(content);
        this._output.WriteLine("Parsed JSON object type: {0}", json.GetProperty("object").GetString());

        var data = json.GetProperty("data");
        this._output.WriteLine("Number of models: {0}", data.GetArrayLength());

        if (data.GetArrayLength() > 0)
        {
            var firstModel = data.EnumerateArray().First();
            this._output.WriteLine("First model ID: {0}", firstModel.GetProperty("id").GetString());
            this._output.WriteLine("First model owned_by: {0}", firstModel.GetProperty("owned_by").GetString());
        }

        Assert.True(data.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Debug_GetModelById_WithSimpleId()
    {
        var listResponse = await this._client.GetAsync("/openai/v1/models");
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var firstModel = listContent.GetProperty("data").EnumerateArray().First();
        var firstModelId = firstModel.GetProperty("id").GetString();

        this._output.WriteLine("Testing with model ID: {0}", firstModelId);

        var response = await this._client.GetAsync($"/openai/v1/models/{firstModelId}");
        var responseContent = await response.Content.ReadAsStringAsync();

        this._output.WriteLine("Response Status: {0}", response.StatusCode);
        this._output.WriteLine("Response Content: {0}", responseContent);

        response.EnsureSuccessStatusCode();
    }
}
