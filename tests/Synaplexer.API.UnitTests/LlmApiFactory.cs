using Synaplexer.API.Configuration;
using Mediator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Synaplexer.Api.Tests;

public class LlmApiFactory : WebApplicationFactory<Program>
{
    public IMediator Mediator { get; } = Substitute.For<IMediator>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Synaplexer:DefaultProvider"] = "OpenAI"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(Mediator);

            // Mock Options
            var options = Substitute.For<IOptions<SynaplexerOptions>>();
            options.Value.Returns(new SynaplexerOptions());
            services.AddSingleton(options);
        });
    }
}
