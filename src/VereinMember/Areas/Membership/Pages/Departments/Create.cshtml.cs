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
public class CreateModel(MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Department Department { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Department.Name))
        {
            ModelState.AddModelError("Department.Name", "Name is required.");
            return Page();
        }

        var existing = await context.Departments
            .AnyAsync(d => d.Name == Department.Name);
        if (existing)
        {
            ModelState.AddModelError("Department.Name", "A department with this name already exists.");
            return Page();
        }

        context.Departments.Add(Department);
        await context.SaveChangesAsync();

        TempData["Success"] = $"Department '{Department.Name}' created successfully.";
        return RedirectToPage("Index");
    }
}