namespace Synaxis.Webhooks.Controllers;

/// <summary>
/// DTO for updating a webhook.
/// </summary>
public class UpdateWebhookRequest
{
    /// <summary>
    /// Gets or sets the URL where webhook events will be sent.
    /// </summary>
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of events this webhook subscribes to.
    /// </summary>
    public List<string> Events { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the webhook is active.
    /// </summary>
    public bool IsActive { get; set; }
}
