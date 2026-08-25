namespace ClubSite.Membership.Models;

public class MembershipFee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public FeeInterval Interval { get; set; } = FeeInterval.Yearly;
    public FeeCategory Category { get; set; } = FeeCategory.Adult;
    public bool IsDefault { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}