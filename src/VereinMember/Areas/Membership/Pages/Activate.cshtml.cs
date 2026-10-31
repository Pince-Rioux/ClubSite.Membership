using VereinMember.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VereinMember.Areas.Membership.Pages;

[ValidateAntiForgeryToken]
public class ActivateModel(
    IInvitationService invitationService,
    IMemberAccountService accountService,
    Microsoft.AspNetCore.Identity.IUserManager userManager) : PageModel
{
    public string Token { get; set; } = string.Empty;
    public string MemberFirstName { get; set; } = string.Empty;
    public string MemberLastName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public bool IsInvalid { get; set; }
    public bool IsCompleted { get; set; }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        Token = token;

        var invitation = await invitationService.ValidateTokenAsync(token);
        if (invitation?.Member == null)
        {
            IsInvalid = true;
            return Page();
        }

        MemberFirstName = invitation.Member.FirstName;
        MemberLastName = invitation.Member.LastName;
        MemberEmail = invitation.Member.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string token)
    {
        Token = token;

        var invitation = await invitationService.ValidateTokenAsync(token);
        if (invitation?.Member == null)
        {
            IsInvalid = true;
            return Page();
        }

        MemberFirstName = invitation.Member.FirstName;
        MemberLastName = invitation.Member.LastName;
        MemberEmail = invitation.Member.Email;

        var password = Request.Form["password"].ToString();
        var confirmPassword = Request.Form["confirmPassword"].ToString();

        // Validate password
        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Bitte geben Sie ein Passwort ein.");
            return Page();
        }

        if (password.Length < 8)
        {
            ModelState.AddModelError(string.Empty, "Das Passwort muss mindestens 8 Zeichen lang sein.");
            return Page();
        }

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Die Passwörter stimmen nicht überein.");
            return Page();
        }

        // Ensure Identity user exists, then set password
        var identityUser = await userManager.FindByEmailAsync(invitation.Member.Email);
        if (identityUser == null)
        {
            identityUser = new MembershipUser();
            identityUser.Email = invitation.Member.Email;
            identityUser.UserName = invitation.Member.Email;
            var createResult = await userManager.CreateUserAsync(identityUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                ModelState.AddModelError(string.Empty, "Identity-User konnte nicht erstellt werden: " + errors);
                return Page();
            }
        }

        // Set password via host Identity
        var success = await accountService.SetPasswordAsync(invitation.Member.Email, password);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "Passwort konnte nicht gesetzt werden. Bitte versuchen Sie es später erneut.");
            _ = invitationService.RecordFailedAttemptAsync(invitation.Id);
            return Page();
        }

        // Mark invitation as completed
        await invitationService.CompleteInvitationAsync(invitation.Id);

        IsCompleted = true;
        return Page();
    }
}