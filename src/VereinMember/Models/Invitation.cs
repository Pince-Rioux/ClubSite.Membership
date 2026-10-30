using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VereinMember.Models;

[Table("Invitations", Schema = "Membership")]
public class Invitation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public int MemberId { get; set; }

    /// <summary>
    /// Cryptographically secure random token (URL-safe base64, 256 bits).
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The email address this invitation was sent to (denormalized for lookup).
    /// </summary>
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this token was attempted (failed validation).
    /// Resets on successful completion.
    /// </summary>
    public int Attempts { get; set; }

    // Navigation
    [ForeignKey(nameof(MemberId))]
    public Member? Member { get; set; }
}