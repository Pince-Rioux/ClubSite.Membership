using VereinMember.Extensions;
using VereinMember.Data;
using VereinMember.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Pages.Departments;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ValidateAntiForgeryToken]
public class DeleteModel(MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Department Department { get; set; } = new();

    public int MemberCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var department = await context.Departments
            .Include(d => d.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        Department = department;
        MemberCount = department.Members.Count(m => m.Status == MemberStatus.Active);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var department = await context.Departments
            .Include(d => d.Members)
            .FirstOrDefaultAsync(d => d.Id == Department.Id);

        if (department == null)
        {
            return NotFound();
        }

        var activeMembers = department.Members.Count(m => m.Status == MemberStatus.Active);
        if (activeMembers > 0)
        {
            ModelState.AddModelError(string.Empty, $"Cannot delete department '{department.Name}' because it has {activeMembers} active member(s) assigned.");
            Department = department;
            MemberCount = activeMembers;
            return Page();
        }

        context.Departments.Remove(department);
        await context.SaveChangesAsync();

        TempData["Success"] = $"Department '{department.Name}' deleted successfully.";
        return RedirectToPage("Index");
    }
}