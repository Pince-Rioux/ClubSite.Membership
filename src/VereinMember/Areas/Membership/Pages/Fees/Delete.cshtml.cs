using VereinMember.Extensions;
using VereinMember.Data;
using VereinMember.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Pages.Fees;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ValidateAntiForgeryToken]
public class DeleteModel(MembershipDbContext context) : PageModel
{
    [BindProperty]
    public MembershipFee Fee { get; set; } = new();

    public int MemberCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var fee = await context.MembershipFees
            .Include(f => f.Members)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fee == null)
        {
            return NotFound();
        }

        Fee = fee;
        MemberCount = fee.Members.Count(m => m.Status == MemberStatus.Active);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var fee = await context.MembershipFees
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == Fee.Id);

        if (fee == null)
        {
            return NotFound();
        }

        var activeMembers = fee.Members.Count(m => m.Status == MemberStatus.Active);
        if (activeMembers > 0)
        {
            ModelState.AddModelError(string.Empty, $"Cannot delete fee '{fee.Name}' because it has {activeMembers} active member(s) assigned.");
            Fee = fee;
            MemberCount = activeMembers;
            return Page();
        }

        context.MembershipFees.Remove(fee);
        await context.SaveChangesAsync();

        TempData["Success"] = $"Fee '{fee.Name}' deleted successfully.";
        return RedirectToPage("Index");
    }
}