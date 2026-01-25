namespace Synaxis.InferenceGateway.Application.Routing;

public interface IModelResolver
{
    ResolutionResult Resolve(string modelId, RequiredCapabilities? required = null);
    Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null);
}
