using Synaplexer.Application.Queries;
using FluentAssertions;

namespace Synaplexer.Application.Tests.Queries;

public class QueryTests
{
    [Fact]
    public void GetProviderStatusQuery_ShouldSetProperties()
    {
        var query = new GetProviderStatusQuery("provider");
        query.ProviderName.Should().Be("provider");
    }

    [Fact]
    public void ListAvailableModelsQuery_ShouldSetProperties()
    {
        var query = new ListAvailableModelsQuery("provider");
        query.Provider.Should().Be("provider");
    }
}
