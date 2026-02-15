// <copyright file="EmailService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Threading.Tasks;
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using MimeKit;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Service for sending emails using SMTP.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailService"/> class.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public EmailService(
            IOptions<EmailOptions> options,
            ILogger<EmailService> logger)
        {
            this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                this._logger.LogInformation("Sending email to: {To}", to);

                using var message = new MimeMessage();
                message.From.Add(new MailboxAddress(this._options.FromName, this._options.FromEmail));
                message.To.Add(new MailboxAddress(string.Empty, to));
                message.Subject = subject;

                message.Body = new TextPart("html")
                {
                    Text = body,
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(this._options.SmtpHost, this._options.SmtpPort, SecureSocketOptions.StartTls).ConfigureAwait(false);
                    await client.AuthenticateAsync(this._options.SmtpUser, this._options.SmtpPassword).ConfigureAwait(false);
                    await client.SendAsync(message).ConfigureAwait(false);
                    await client.DisconnectAsync(true).ConfigureAwait(false);
                }

                this._logger.LogInformation("Email sent successfully to: {To}", to);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error sending email to: {To}", to);
                throw new InvalidOperationException($"Failed to send email to {to}", ex);
            }
        }

        /// <inheritdoc/>
        public Task SendVerificationEmailAsync(string to, string verificationUrl)
        {
            var subject = "Verify your email address";
            var body = $@"
                <html>
                <body>
                    <h2>Welcome to Synaxis!</h2>
                    <p>Please verify your email address by clicking the link below:</p>
                    <p><a href='{verificationUrl}'>Verify Email</a></p>
                    <p>If you didn't create an account, please ignore this email.</p>
                    <p>This link will expire in 24 hours.</p>
                </body>
                </html>";

            return this.SendEmailAsync(to, subject, body);
        }

        /// <inheritdoc/>
        public Task SendPasswordResetEmailAsync(string to, string resetUrl)
        {
            var subject = "Reset your password";
            var body = $@"
                <html>
                <body>
                    <h2>Password Reset Request</h2>
                    <p>You requested a password reset for your Synaxis account.</p>
                    <p>Click the link below to reset your password:</p>
                    <p><a href='{resetUrl}'>Reset Password</a></p>
                    <p>If you didn't request a password reset, please ignore this email.</p>
                    <p>This link will expire in 1 hour.</p>
                </body>
                </html>";

            return this.SendEmailAsync(to, subject, body);
        }

        /// <inheritdoc/>
        public Task SendMfaSetupEmailAsync(string to, string secret)
        {
            var subject = "MFA Setup Confirmation";
            var body = $@"
                <html>
                <body>
                    <h2>MFA Setup Confirmation</h2>
                    <p>Multi-factor authentication has been enabled for your account.</p>
                    <p>Your MFA secret key is: <strong>{secret}</strong></p>
                    <p>Please store this key in a safe place. You can use it to recover your account if you lose access to your authenticator app.</p>
                    <p>If you didn't enable MFA, please contact support immediately.</p>
                </body>
                </html>";

            return this.SendEmailAsync(to, subject, body);
        }
    }
}
