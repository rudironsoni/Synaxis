using ContextSavvy.LlmProviders.Domain.ValueObjects;

namespace ContextSavvy.LlmProviders.Domain.Entities;

/// <summary>
/// Represents credentials or account details for an LLM provider.
/// </summary>
public class ProviderAccount
{
    /// <summary>
    /// Unique identifier for the account.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The type of provider this account belongs to.
    /// </summary>
    public ProviderType Provider { get; private set; }

    /// <summary>
    /// Email address associated with the account.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Indicates whether the account is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Timestamp of when the account was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Timestamp until which the account is in cooldown and cannot be used.
    /// </summary>
    public DateTime? CooldownUntil { get; private set; }

    public ProviderAccount(Guid id, ProviderType provider, string email)
    {
        Id = id;
        Provider = provider;
        Email = email;
        IsActive = true;
    }

    /// <summary>
    /// Checks if the account is available for use (active and not in cooldown).
    /// </summary>
    /// <returns>True if the account can be used; otherwise, false.</returns>
    public bool CanUse()
    {
        return IsActive && (CooldownUntil == null || CooldownUntil <= DateTime.UtcNow);
    }

    /// <summary>
    /// Marks the account as used and updates the last used timestamp.
    /// </summary>
    public void MarkUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets a cooldown period for the account.
    /// </summary>
    /// <param name="until">The timestamp until which the account should remain in cooldown.</param>
    public void SetCooldown(DateTime until)
    {
        CooldownUntil = until;
    }

    /// <summary>
    /// Deactivates the account.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activates the account.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }
}
