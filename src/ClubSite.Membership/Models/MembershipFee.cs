using System.ComponentModel.DataAnnotations;

namespace ClubSite.Membership.Models;

public class MembershipFee
{
    public int Id { get; set; }

    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Betrag")]
    public decimal Amount { get; set; }

    [Display(Name = "Intervall")]
    public FeeInterval Interval { get; set; } = FeeInterval.Yearly;

    [Display(Name = "Kategorie")]
    public FeeCategory Category { get; set; } = FeeCategory.Adult;

    [Display(Name = "Standard")]
    public bool IsDefault { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}