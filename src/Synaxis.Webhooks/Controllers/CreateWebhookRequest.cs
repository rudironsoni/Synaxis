// <copyright file="CreateWebhookRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Controllers
{
    using System.ComponentModel.DataAnnotations;

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
        public IList<string> Events { get; set; } = new List<string>();
    }
}
