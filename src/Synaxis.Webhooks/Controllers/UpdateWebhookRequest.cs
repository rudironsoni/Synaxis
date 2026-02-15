// <copyright file="UpdateWebhookRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Controllers
{
    using System.ComponentModel.DataAnnotations;

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
        public IList<string> Events { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the webhook is active.
        /// </summary>
        public bool? IsActive { get; set; }
    }
}
