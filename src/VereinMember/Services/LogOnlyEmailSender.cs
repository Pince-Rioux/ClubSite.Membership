using Microsoft.Extensions.Logging;

namespace VereinMember.Services;

/// <summary>
/// Default fallback implementation of <see cref="IEmailSender"/> that logs emails to the console.
/// The host (ClubSite) should register its own <see cref="IEmailSender"/>
/// that sends real emails via SMTP, SendGrid, or the host's existing mail service.
/// </summary>
public class LogOnlyEmailSender : IEmailSender
{
    private readonly ILogger<LogOnlyEmailSender> _logger;

    public LogOnlyEmailSender(ILogger<LogOnlyEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation(
            "[LOG-ONLY EMAIL] To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body);

        return Task.CompletedTask;
    }
}