namespace ClubSite.Membership.Models;

public class FamilyGroup
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}