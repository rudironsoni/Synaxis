var builder = WebApplication.CreateBuilder(args);

// Configure YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromMemory(new[] {
        new Yarp.ReverseProxy.Configuration.RouteConfig {
            RouteId = "api_route",
            ClusterId = "inference_cluster",
            Match = new Yarp.ReverseProxy.Configuration.RouteMatch { Path = "/v1/{**catch-all}" }
        }
    }, new[] {
        new Yarp.ReverseProxy.Configuration.ClusterConfig {
            ClusterId = "inference_cluster",
            Destinations = new System.Collections.Generic.Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig> {
                { "dest1", new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = "http://localhost:5000" } }
            }
        }
    });

var app = builder.Build();

// Serve static files from wwwroot (will contain built SPA)
app.UseStaticFiles();

// Map reverse proxy for /v1
app.MapReverseProxy();

// Fallback to index.html for SPA routes
app.MapFallbackToFile("index.html");

app.Run();
