using ClubSite.Membership.Extensions;
using ClubSite.Membership.Models;
using ClubSite.Membership.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClubSite.Membership.Areas.Membership.Pages.Members;

[Authorize(Policy = AuthorizationPolicies.MemberDelete)]
[ValidateAntiForgeryToken]
public class DeleteModel(IMemberService memberService) : PageModel
{
    [BindProperty]
    public Member? Member { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Member = await memberService.GetByIdAsync(id);
        if (Member == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Member == null)
        {
            return NotFound();
        }

        var performedBy = User.Identity?.Name ?? "system";
        var result = await memberService.SoftDeleteAsync(Member.Id, performedBy);

        if (!result)
        {
            return NotFound();
        }

        TempData["Success"] = $"Member '{Member.FirstName} {Member.LastName}' has been soft-deleted.";
        return RedirectToPage("Index");
    }
}