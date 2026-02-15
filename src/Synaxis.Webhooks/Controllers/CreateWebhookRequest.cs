using System.ComponentModel.DataAnnotations;

namespace Synaxis.Webhooks.Controllers;

/// <summary>
/// DTO for creating a webhook.
/// </summary>
public class CreateWebhookRequest
{
    /// <summary>
    /// Gets or sets the URL where webhook events will be sent.
    /// </summary>
    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of events this webhook subscribes to.
    /// </summary>
    [Required]
    public List<string> Events { get; set; } = new List<string>();
}
