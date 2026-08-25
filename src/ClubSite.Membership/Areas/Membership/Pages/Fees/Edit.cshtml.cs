using ClubSite.Membership.Extensions;
using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Areas.Membership.Pages.Fees;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ValidateAntiForgeryToken]
public class EditModel(MembershipDbContext context) : PageModel
{
    [BindProperty]
    public MembershipFee Fee { get; set; } = new();

    public List<SelectListItem> IntervalOptions { get; set; } = [];
    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var fee = await context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fee == null)
        {
            return NotFound();
        }

        Fee = fee;
        LoadOptions();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Fee.Name))
        {
            ModelState.AddModelError("Fee.Name", "Name is required.");
            LoadOptions();
            return Page();
        }

        if (Fee.Amount <= 0)
        {
            ModelState.AddModelError("Fee.Amount", "Amount must be greater than zero.");
            LoadOptions();
            return Page();
        }

        var existing = await context.MembershipFees.FindAsync(Fee.Id);
        if (existing == null)
        {
            return NotFound();
        }

        // If setting as default, unset existing default for this category
        if (Fee.IsDefault && !existing.IsDefault)
        {
            var currentDefault = await context.MembershipFees
                .FirstOrDefaultAsync(f => f.IsDefault && f.Category == Fee.Category && f.Id != Fee.Id);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
            }
        }

        existing.Name = Fee.Name;
        existing.Amount = Fee.Amount;
        existing.Interval = Fee.Interval;
        existing.Category = Fee.Category;
        existing.IsDefault = Fee.IsDefault;
        await context.SaveChangesAsync();

        TempData["Success"] = $"Fee '{existing.Name}' updated successfully.";
        return RedirectToPage("Index");
    }

    private void LoadOptions()
    {
        IntervalOptions = Enum.GetValues<FeeInterval>()
            .Select(i => new SelectListItem { Value = i.ToString(), Text = i.ToString() })
            .ToList();

        CategoryOptions = Enum.GetValues<FeeCategory>()
            .Select(c => new SelectListItem { Value = c.ToString(), Text = c.ToString() })
            .ToList();
    }
}