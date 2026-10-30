using VereinMember.Extensions;
using VereinMember.Data;
using VereinMember.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Pages.Fees;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ValidateAntiForgeryToken]
public class CreateModel(MembershipDbContext context) : PageModel
{
    [BindProperty]
    public MembershipFee Fee { get; set; } = new();

    public List<SelectListItem> IntervalOptions { get; set; } = [];
    public List<SelectListItem> CategoryOptions { get; set; } = [];

    public void OnGet()
    {
        LoadOptions();
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

        // If setting as default, unset existing default
        if (Fee.IsDefault)
        {
            var currentDefault = await context.MembershipFees
                .FirstOrDefaultAsync(f => f.IsDefault && f.Category == Fee.Category);
            if (currentDefault != null)
            {
                currentDefault.IsDefault = false;
            }
        }

        context.MembershipFees.Add(Fee);
        await context.SaveChangesAsync();

        TempData["Success"] = $"Fee '{Fee.Name}' created successfully.";
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