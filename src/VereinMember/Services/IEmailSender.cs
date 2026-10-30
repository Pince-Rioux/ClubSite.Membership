namespace VereinMember.Services;

/// <summary>
/// Abstraction for sending emails from the Membership module.
/// The RCL provides a default log-only implementation.
/// The host (ClubSite) registers its own implementation that sends real emails
/// via the host's mail infrastructure (SMTP, SendGrid, etc.).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email to a single recipient.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Plain-text email body.</param>
    Task SendAsync(string to, string subject, string body);
}