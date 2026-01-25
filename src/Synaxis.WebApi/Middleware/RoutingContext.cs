namespace Synaxis.WebApi.Middleware;
public record RoutingContext(string RequestedModel, string ResolvedCanonicalId, string Provider);
