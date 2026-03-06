// Stub interface for backward compatibility with tests
// Email functionality was removed due to MimeKit security vulnerabilities
namespace Synaxis.Core.Contracts;

public interface IEmailService
{
    // Stub methods for test compatibility - no actual implementation
    Task SendVerificationEmailAsync(string email, string verificationUrl);
    Task SendPasswordResetEmailAsync(string email, string resetUrl);
}
