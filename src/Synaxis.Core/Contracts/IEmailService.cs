// <copyright file="IEmailService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for sending emails.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send an email asynchronously.
        /// </summary>
        /// <param name="to">The recipient email address.</param>
        /// <param name="subject">The email subject.</param>
        /// <param name="body">The email body (HTML).</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendEmailAsync(string to, string subject, string body);

        /// <summary>
        /// Send an email verification email.
        /// </summary>
        /// <param name="to">The recipient email address.</param>
        /// <param name="verificationUrl">The verification URL.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendVerificationEmailAsync(string to, string verificationUrl);

        /// <summary>
        /// Send a password reset email.
        /// </summary>
        /// <param name="to">The recipient email address.</param>
        /// <param name="resetUrl">The password reset URL.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendPasswordResetEmailAsync(string to, string resetUrl);

        /// <summary>
        /// Send an MFA setup email.
        /// </summary>
        /// <param name="to">The recipient email address.</param>
        /// <param name="secret">The MFA secret.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendMfaSetupEmailAsync(string to, string secret);
    }
}
