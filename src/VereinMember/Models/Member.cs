using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VereinMember.Models;

public class Member
{
    [Display(Name = "Mitglieds-Nr.")]
    public int Id { get; set; }

    [Display(Name = "Vorname")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Nachname")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-Mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [Display(Name = "Geburtsdatum")]
    public DateTime BirthDate { get; set; }

    [Display(Name = "Straße")]
    public string? Street { get; set; }

    [Display(Name = "Ort")]
    public string? City { get; set; }

    [Display(Name = "PLZ")]
    public string? ZipCode { get; set; }

    [Display(Name = "Land")]
    public string? Country { get; set; }

    public string? BankIban { get; set; }
    public string? BankBic { get; set; }

    [Display(Name = "Beigetreten am")]
    public DateTime JoinedAt { get; set; }

    [Display(Name = "Ausgetreten am")]
    public DateTime? LeftAt { get; set; }

    [Display(Name = "Status")]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [Display(Name = "Abteilung")]
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    [Display(Name = "Beitragsgruppe")]
    public int MembershipFeeId { get; set; }
    public MembershipFee? MembershipFee { get; set; }

    [Display(Name = "Notizen")]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public byte[] ConcurrencyToken { get; set; } = [];

    [Display(Name = "Erziehungsberechtigter")]
    public int? GuardianMemberId { get; set; }
    [ForeignKey(nameof(GuardianMemberId))]
    public Member? GuardianMember { get; set; }
    [InverseProperty(nameof(GuardianMember))]
    public ICollection<Member> DependentMembers { get; set; } = new List<Member>();

    [Display(Name = "Familien-Gruppe")]
    public int? FamilyGroupId { get; set; }
    [ForeignKey(nameof(FamilyGroupId))]
    public FamilyGroup? FamilyGroup { get; set; }
}