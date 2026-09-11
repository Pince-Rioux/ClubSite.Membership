using ClubSite.Membership.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ClubSite.Membership.Services;

/// <summary>
/// Default implementation of IMemberAccountService that uses the RCL's own Identity infrastructure.
/// No dependency on the host application's Identity or Piranha.
/// </summary>
public class DefaultMemberAccountService : IMemberAccountService
{
    private readonly UserManager<MembershipUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<DefaultMemberAccountService> _logger;

    public DefaultMemberAccountService(
        UserManager<MembershipUser> userManager,
        IEmailSender emailSender,
        ILogger<DefaultMemberAccountService> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<bool> SetPasswordAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("SetPasswordAsync: no Identity user found for {Email}", email);
            return false;
        }

        // Remove password if already set, then add new one
        if (await _userManager.HasPasswordAsync(user))
        {
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                _logger.LogError("SetPasswordAsync: failed to remove old password for {Email}: {Errors}",
                    email, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                return false;
            }
        }

        var addResult = await _userManager.AddPasswordAsync(user, password);
        if (!addResult.Succeeded)
        {
            _logger.LogError("SetPasswordAsync: failed to set password for {Email}: {Errors}",
                email, string.Join(", ", addResult.Errors.Select(e => e.Description)));
            return false;
        }

        _logger.LogInformation("SetPasswordAsync: password set for {Email}", email);
        return true;
    }

    public async Task SendInvitationEmailAsync(string email, string memberName, string inviteUrl)
    {
        var subject = $"Willkommen beim VC Neusäß – Registrierung abschließen";
        var body = $"Hallo {memberName},\n\n" +
                   $"Sie wurden beim Volleyballclub Neusäß e. V. registriert.\n" +
                   $"Bitte klicken Sie auf den folgenden Link, um Ihr Passwort zu setzen und Ihren Account zu aktivieren:\n\n" +
                   $"{inviteUrl}\n\n" +
                   $"Der Link ist 7 Tage gültig.\n\n" +
                   $"Sportliche Grüße,\nIhr VC Neusäß";

        await _emailSender.SendAsync(email, subject, body);

        _logger.LogInformation(
            "Invitation email sent to {Name} <{Email}>: {Url}",
            memberName, email, inviteUrl);
    }
}