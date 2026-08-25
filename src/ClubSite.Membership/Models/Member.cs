using System.ComponentModel.DataAnnotations.Schema;

namespace ClubSite.Membership.Models;

public class Member
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime BirthDate { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? BankIban { get; set; }
    public string? BankBic { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public MemberStatus Status { get; set; } = MemberStatus.Active;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int MembershipFeeId { get; set; }
    public MembershipFee? MembershipFee { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public byte[] ConcurrencyToken { get; set; } = [];

    // --- NEW FIELDS ---
    public int? GuardianMemberId { get; set; }
    [ForeignKey(nameof(GuardianMemberId))]
    public Member? GuardianMember { get; set; }
    [InverseProperty(nameof(GuardianMember))]
    public ICollection<Member> DependentMembers { get; set; } = new List<Member>();

    public int? FamilyGroupId { get; set; }
    [ForeignKey(nameof(FamilyGroupId))]
    public FamilyGroup? FamilyGroup { get; set; }
}