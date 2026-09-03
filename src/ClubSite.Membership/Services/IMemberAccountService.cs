namespace ClubSite.Membership.Services;

/// <summary>
/// Abstraction for operations that require the host application's Identity infrastructure.
/// The host (ClubSite) implements this interface to provide password management
/// and email sending, which depend on Piranha Identity and the host's MailService.
/// </summary>
public interface IMemberAccountService
{
    /// <summary>
    /// Sets the password for a member's Identity account.
    /// Called when a member accepts an invitation (Magic-Link) and sets their password.
    /// Should create the Identity user if one doesn't exist yet,
    /// hash the password, and persist it.
    /// </summary>
    /// <param name="email">The member's email address (Identity username).</param>
    /// <param name="password">The new password to set.</param>
    /// <returns>True on success, false on failure.</returns>
    Task<bool> SetPasswordAsync(string email, string password);

    /// <summary>
    /// Sends an invitation email to a new member with a Magic-Link.
    /// The link contains the token for password set.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="memberName">Display name for the salutation.</param>
    /// <param name="inviteUrl">Full URL to the activation page with token.</param>
    Task SendInvitationEmailAsync(string email, string memberName, string inviteUrl);
}