namespace Synaxis.InferenceGateway.Application.Routing;

public interface IModelResolver
{
    // Deprecated sync method
    ResolutionResult Resolve(string modelId, RequiredCapabilities? required = null);

    Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null, Guid? tenantId = null);
}
