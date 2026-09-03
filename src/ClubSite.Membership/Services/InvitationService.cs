using System.Security.Cryptography;
using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Services;

public class InvitationService(MembershipDbContext context) : IInvitationService
{
    private const int TokenByteLength = 32; // 256 bits
    private const int InvitationExpiryDays = 7;
    private const int MaxAttempts = 5;

    public async Task<Invitation> CreateInvitationAsync(int memberId, string email)
    {
        // Cancel any pending invitations for this member
        await CancelPendingInvitationsAsync(memberId);

        var token = GenerateToken();

        var invitation = new Invitation
        {
            MemberId = memberId,
            Token = token,
            Email = email,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(InvitationExpiryDays),
            CompletedAt = null,
            Attempts = 0
        };

        context.Invitations.Add(invitation);
        await context.SaveChangesAsync();

        return invitation;
    }

    public async Task<Invitation?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var invitation = await context.Invitations
            .Include(i => i.Member)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
            return null;

        // Token found — check conditions
        if (invitation.CompletedAt.HasValue)
            return null;

        if (invitation.ExpiresAt < DateTime.UtcNow)
            return null;

        if (invitation.Attempts >= MaxAttempts)
        {
            // Token is locked out due to too many failed attempts
            return null;
        }

        return invitation;
    }

    public async Task CompleteInvitationAsync(Guid invitationId)
    {
        var invitation = await context.Invitations.FindAsync(invitationId);
        if (invitation == null)
            return;

        invitation.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<Invitation>> GetPendingInvitationsAsync(string email)
    {
        return await context.Invitations
            .Where(i => i.Email == email
                        && i.CompletedAt == null
                        && i.ExpiresAt >= DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task CancelPendingInvitationsAsync(int memberId)
    {
        var pending = await context.Invitations
            .Where(i => i.MemberId == memberId && i.CompletedAt == null)
            .ToListAsync();

        foreach (var inv in pending)
        {
            inv.ExpiresAt = DateTime.UtcNow; // expire immediately
        }

        if (pending.Count > 0)
            await context.SaveChangesAsync();
    }

    /// <summary>
    /// Increments the attempt counter for an invitation token.
    /// Called when the user submits an invalid password or the token validation fails.
    /// </summary>
    public async Task RecordFailedAttemptAsync(Guid invitationId)
    {
        var invitation = await context.Invitations.FindAsync(invitationId);
        if (invitation != null)
        {
            invitation.Attempts++;
            await context.SaveChangesAsync();
        }
    }

    private static string GenerateToken()
    {
        var bytes = new byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        // URL-safe base64 for .NET 8 compatibility
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}