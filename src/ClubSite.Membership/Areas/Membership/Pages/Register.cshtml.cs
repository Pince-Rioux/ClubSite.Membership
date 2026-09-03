using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using ClubSite.Membership.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Areas.Membership.Pages;

[ValidateAntiForgeryToken]
public class RegisterModel(
    IMemberService memberService,
    IInvitationService invitationService,
    IMemberAccountService accountService,
    IValidator<Member> validator,
    MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Member Member { get; set; } = new();

    /// <summary>
    /// Guardian data for Youth members (not persisted as Member — saved as admin note for now).
    /// In a full implementation, this would create a guardian Member record or link to existing.
    /// </summary>
    [BindProperty]
    public string? GuardianFirstName { get; set; }

    [BindProperty]
    public string? GuardianLastName { get; set; }

    [BindProperty]
    public string? GuardianEmail { get; set; }

    /// <summary>
    /// Additional family members (used when FeeCategory = Family).
    /// </summary>
    [BindProperty]
    public List<FamilyMemberItem> FamilyMembers { get; set; } = [];

    public List<SelectListItem> FeeOptions { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var selectedFee = await context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == Member.MembershipFeeId);

        if (selectedFee == null)
        {
            ModelState.AddModelError("Member.MembershipFeeId", "Bitte wählen Sie eine Beitragsgruppe.");
            await LoadOptionsAsync();
            return Page();
        }

        // For self-service registration, Family members use CreateFamilyAsync
        if (selectedFee.Category == FeeCategory.Family)
        {
            return await HandleFamilyRegistrationAsync();
        }

        // Validate member
        var validationResult = await validator.ValidateAsync(Member);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError($"Member.{error.PropertyName}", error.ErrorMessage);
            }
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            // Create the member
            Member.Status = MemberStatus.Active;
            Member.JoinedAt = DateTime.UtcNow;

            var created = await memberService.CreateAsync(Member, Member.Email);

            // Create invitation token
            var invitation = await invitationService.CreateInvitationAsync(created.Id, created.Email);

            // Send invitation email
            var inviteUrl = Url.PageLink("Activate", null, new { token = invitation.Token })
                            ?? $"/Membership/Activate?token={invitation.Token}";

            await accountService.SendInvitationEmailAsync(
                created.Email,
                $"{created.FirstName} {created.LastName}",
                inviteUrl);

            TempData["Success"] = $"Registrierung erfolgreich! Bitte prüfen Sie Ihr E-Mail-Postfach ({created.Email}) und klicken Sie auf den Bestätigungslink.";
            return RedirectToPage("RegisterConfirmation", new { email = created.Email });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Registrierung fehlgeschlagen: {ex.Message}");
            await LoadOptionsAsync();
            return Page();
        }
    }

    private async Task<IActionResult> HandleFamilyRegistrationAsync()
    {
        // Validate main member
        Member.GuardianMemberId = null;
        var mainResult = await validator.ValidateAsync(Member);
        foreach (var error in mainResult.Errors)
        {
            if (error.PropertyName != "FamilyGroupId" && error.PropertyName != "GuardianMemberId")
                ModelState.AddModelError($"Member.{error.PropertyName}", error.ErrorMessage);
        }

        // Convert sub-members
        var subMembers = new List<Member>();
        for (int i = 0; i < FamilyMembers.Count; i++)
        {
            var fm = FamilyMembers[i];
            if (string.IsNullOrWhiteSpace(fm.FirstName) && string.IsNullOrWhiteSpace(fm.LastName))
                continue;

            var subMember = new Member
            {
                FirstName = fm.FirstName.Trim(),
                LastName = fm.LastName.Trim(),
                Email = fm.Email.Trim(),
                BirthDate = fm.BirthDate,
                MembershipFeeId = Member.MembershipFeeId,
                Street = Member.Street,
                City = Member.City,
                ZipCode = Member.ZipCode,
                Country = Member.Country,
            };

            var subResult = await validator.ValidateAsync(subMember);
            if (!subResult.IsValid)
            {
                foreach (var error in subResult.Errors)
                    ModelState.AddModelError($"FamilyMembers[{i}].{error.PropertyName}", error.ErrorMessage);
            }
            subMembers.Add(subMember);
        }

        if (!ModelState.IsValid || subMembers.Count < 1)
        {
            if (subMembers.Count < 1)
                ModelState.AddModelError(string.Empty, "Bitte fügen Sie mindestens ein weiteres Familienmitglied hinzu.");
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            // Create all family members
            var createdMembers = await memberService.CreateFamilyAsync(Member, subMembers, Member.Email);

            // Create invitations for all family members
            foreach (var cm in createdMembers)
            {
                var invitation = await invitationService.CreateInvitationAsync(cm.Id, cm.Email);
                var inviteUrl = Url.PageLink("Activate", null, new { token = invitation.Token })
                                ?? $"/Membership/Activate?token={invitation.Token}";

                await accountService.SendInvitationEmailAsync(
                    cm.Email,
                    $"{cm.FirstName} {cm.LastName}",
                    inviteUrl);
            }

            TempData["Success"] = $"{createdMembers.Count} Familienmitglieder wurden registriert. Jedes Mitglied erhält eine E-Mail mit einem Bestätigungslink.";
            return RedirectToPage("RegisterConfirmation", new { email = Member.Email });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Registrierung fehlgeschlagen: {ex.Message}");
            await LoadOptionsAsync();
            return Page();
        }
    }

    private async Task LoadOptionsAsync()
    {
        FeeOptions = await context.MembershipFees
            .Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Name + " (" + f.Amount.ToString("C") + ")",
                Group = new SelectListGroup { Name = f.Category.ToString() }
            })
            .ToListAsync();
    }

    public class FamilyMemberItem
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
    }
}