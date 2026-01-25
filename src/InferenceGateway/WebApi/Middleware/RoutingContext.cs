namespace Synaxis.InferenceGateway.WebApi.Middleware;
public record RoutingContext(string RequestedModel, string ResolvedCanonicalId, string Provider);
