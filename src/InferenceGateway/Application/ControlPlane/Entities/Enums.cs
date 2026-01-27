namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public enum TenantRegion
{
    Us,
    Eu,
    Sa
}

public enum TenantStatus
{
    Active,
    Suspended
}

public enum ProjectStatus
{
    Active,
    Archived
}

public enum UserRole
{
    Owner,
    Admin,
    Developer,
    Readonly
}

public enum ApiKeyStatus
{
    Active,
    Revoked
}

public enum OAuthAccountStatus
{
    Active,
    Revoked
}

public enum ProviderAccountStatus
{
    Active,
    Cooldown,
    Disabled
}

public enum DeviationStatus
{
    Open,
    Mitigated,
    Closed
}
