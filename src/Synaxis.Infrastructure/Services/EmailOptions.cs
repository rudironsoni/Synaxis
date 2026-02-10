// <copyright file="EmailOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Configuration options for email service.
    /// </summary>
    public class EmailOptions
    {
        /// <summary>
        /// Gets or sets the SMTP host.
        /// </summary>
        public string SmtpHost { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP port.
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Gets or sets the SMTP username.
        /// </summary>
        public string SmtpUser { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the SMTP password.
        /// </summary>
        public string SmtpPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the from email address.
        /// </summary>
        public string FromEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the from name.
        /// </summary>
        public string FromName { get; set; } = "Synaxis";
    }
}
