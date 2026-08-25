using ClubSite.Membership.Extensions;
using ClubSite.Membership.Models;
using ClubSite.Membership.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClubSite.Membership.Areas.Membership.Pages.Members;

[Authorize(Policy = AuthorizationPolicies.MemberView)]
public class DetailsModel(IMemberService memberService) : PageModel
{
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
}