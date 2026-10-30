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
public class EditModel(MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Department Department { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var department = await context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
        {
            return NotFound();
        }

        Department = department;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Department.Name))
        {
            ModelState.AddModelError("Department.Name", "Name is required.");
            return Page();
        }

        var existing = await context.Departments
            .AnyAsync(d => d.Name == Department.Name && d.Id != Department.Id);
        if (existing)
        {
            ModelState.AddModelError("Department.Name", "A department with this name already exists.");
            return Page();
        }

        var department = await context.Departments.FindAsync(Department.Id);
        if (department == null)
        {
            return NotFound();
        }

        department.Name = Department.Name;
        department.Description = Department.Description;
        await context.SaveChangesAsync();

        TempData["Success"] = $"Department '{department.Name}' updated successfully.";
        return RedirectToPage("Index");
    }
}