namespace ClubSite.Membership.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}