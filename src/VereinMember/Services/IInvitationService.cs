using VereinMember.Models;

namespace VereinMember.Services;

/// <summary>
/// Manages invitation tokens for self-service registration and admin invite flows.
/// Tokens are cryptographically secure, time-limited (7 days), and single-use.
/// </summary>
public interface IInvitationService
{
    /// <summary>
    /// Creates an invitation for the given member.
    /// Generates a crypto-random token (256-bit, URL-safe base64),
    /// expires in 7 days.
    /// </summary>
    Task<Invitation> CreateInvitationAsync(int memberId, string email);

    /// <summary>
    /// Validates a token. Returns the invitation if valid, null otherwise.
    /// A token is valid if:
    /// - It exists in the database
    /// - It has not expired
    /// - It has not been completed
    /// - It has fewer than 5 failed attempts
    /// Returns null and increments Attempts on invalid/missing/expired tokens.
    /// </summary>
    Task<Invitation?> ValidateTokenAsync(string token);

    /// <summary>
    /// Marks an invitation as completed (sets CompletedAt).
    /// This consumes the token — it cannot be reused.
    /// </summary>
    Task CompleteInvitationAsync(Guid invitationId);

    /// <summary>
    /// Returns all pending (unexpired, not completed) invitations for a given email.
    /// </summary>
    Task<List<Invitation>> GetPendingInvitationsAsync(string email);

    /// <summary>
    /// Cancels (expires) all pending invitations for a member.
    /// Used when a new invitation is created for the same member.
    /// </summary>
    Task CancelPendingInvitationsAsync(int memberId);

    /// <summary>
    /// Records a failed validation attempt for an invitation.
    /// After MaxAttempts failures, the token is locked out.
    /// </summary>
    Task RecordFailedAttemptAsync(Guid invitationId);
}