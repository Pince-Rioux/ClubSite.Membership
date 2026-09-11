using System.ComponentModel.DataAnnotations;

namespace ClubSite.Membership.Models;

public class Department
{
    public int Id { get; set; }

    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Beschreibung")]
    public string? Description { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}