using System.ComponentModel.DataAnnotations;

namespace VereinMember.Models;

public class FamilyGroup
{
    public int Id { get; set; }

    [Display(Name = "Name")]
    public string? Name { get; set; }

    [Display(Name = "Erstellt am")]
    public DateTime CreatedAt { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
}