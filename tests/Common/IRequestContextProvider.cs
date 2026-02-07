namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for request context information.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IRequestContextProvider
{
    string GetTenantId();

    string GetUserId();

    string GetSessionId();
}
