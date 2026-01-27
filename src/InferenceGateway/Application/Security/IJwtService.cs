using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Application.Security;

public interface IJwtService
{
    string GenerateToken(User user);
}