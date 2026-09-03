using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using ClubSite.Membership.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Areas.Membership.Pages;

[Authorize]
[ValidateAntiForgeryToken]
public class ProfileModel(
    IMemberService memberService,
    MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Member Member { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrEmpty(email))
        {
            return Challenge();
        }

        var member = await memberService.GetByEmailAsync(email);
        if (member == null)
        {
            return NotFound();
        }

        Member = member;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrEmpty(email))
        {
            return Challenge();
        }

        // Verify the logged-in user owns this profile
        var currentMember = await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == Member.Id);

        if (currentMember == null || currentMember.Email != email)
        {
            return Forbid();
        }

        // Basic validation
        if (string.IsNullOrWhiteSpace(Member.FirstName))
            ModelState.AddModelError("Member.FirstName", "Vorname ist erforderlich.");
        if (string.IsNullOrWhiteSpace(Member.LastName))
            ModelState.AddModelError("Member.LastName", "Nachname ist erforderlich.");

        if (!ModelState.IsValid)
        {
            // Reload the member with navigation properties for display
            Member = await memberService.GetByEmailAsync(email) ?? Member;
            return Page();
        }

        try
        {
            var performedBy = User.Identity?.Name ?? "system";
            var updated = await memberService.UpdateOwnProfileAsync(Member.Id, Member, performedBy);
            TempData["Success"] = "Profil erfolgreich aktualisiert.";
            return RedirectToPage();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            Member = await memberService.GetByEmailAsync(email) ?? Member;
            return Page();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}