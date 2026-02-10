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

        public EmailService(
            IOptions<EmailOptions> options,
            ILogger<EmailService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                _logger.LogInformation("Sending email to: {To}", to);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
                message.To.Add(new MailboxAddress(string.Empty, to));
                message.Subject = subject;

                message.Body = new TextPart("html")
                {
                    Text = body
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_options.SmtpUser, _options.SmtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("Email sent successfully to: {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to: {To}", to);
                throw new InvalidOperationException($"Failed to send email to {to}", ex);
            }
        }

        public async Task SendVerificationEmailAsync(string to, string verificationUrl)
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

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetUrl)
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

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendMfaSetupEmailAsync(string to, string secret)
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

            await SendEmailAsync(to, subject, body);
        }
    }
}
