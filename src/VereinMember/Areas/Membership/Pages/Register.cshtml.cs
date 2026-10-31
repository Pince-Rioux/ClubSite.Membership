using VereinMember.Data;
using VereinMember.Models;
using VereinMember.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Pages;

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
    /// Cross-browser date input: day/month/year selects.
    /// Combined into Member.BirthDate before validation.
    /// </summary>
    [BindProperty]
    public int? BirthDay { get; set; }

    [BindProperty]
    public int? BirthMonth { get; set; }

    [BindProperty]
    public int? BirthYear { get; set; }

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

    [BindProperty]
    public string? GuardianPhone { get; set; }

    /// <summary>
    /// Additional family members (used when FeeCategory = Family).
    /// </summary>
    [BindProperty]
    public List<FamilyMemberItem> FamilyMembers { get; set; } = [];

    [BindProperty]
    public bool PrivacyConsented { get; set; }

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

        // Compose BirthDate from cross-browser selects
        if (!TryComposeBirthDate()) return Page();

        // Validate email uniqueness
        var existingEmail = await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email == Member.Email);
        if (existingEmail != null)
        {
            ModelState.AddModelError("Member.Email", "Diese E-Mail-Adresse wird bereits verwendet.");
            await LoadOptionsAsync();
            return Page();
        }

        // Validate privacy consent
        if (!PrivacyConsented)
        {
            ModelState.AddModelError("PrivacyConsented", "Bitte stimmen Sie der Datenschutzerklärung zu.");
            await LoadOptionsAsync();
            return Page();
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
            if (PrivacyConsented)
            {
                Member.PrivacyConsentedAt = DateTime.UtcNow;
            }

            var created = await memberService.CreateAsync(Member, Member.Email);

            // Send invitation email with magic link
            var invitation = await invitationService.CreateInvitationAsync(created.Id, created.Email);
            var inviteUrl = Url.PageLink("Activate", null, new { token = invitation.Token });
            await accountService.SendInvitationEmailAsync(
                created.Email,
                $"{created.FirstName} {created.LastName}",
                inviteUrl);

            // Privacy consent recorded on Member before CreateAsync (see below)

            TempData["Success"] = $"Registrierung erfolgreich! Bitte prüfen Sie Ihr E-Mail-Postfach ({created.Email}).";
            return RedirectToPage("RegisterConfirmation", new { email = created.Email });
        }
        catch (Exception ex)
        {
            var detail = ex.Message;
            if (ex.InnerException != null)
                detail += " | Inner: " + ex.InnerException.Message;
            ModelState.AddModelError(string.Empty, $"Registrierung fehlgeschlagen: {detail}");
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

        // Validate email uniqueness
        var existingEmail = await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email == Member.Email);
        if (existingEmail != null)
        {
            ModelState.AddModelError("Member.Email", "Diese E-Mail-Adresse wird bereits verwendet.");
            await LoadOptionsAsync();
            return Page();
        }

        // Validate email uniqueness for main member
        var existingEmail = await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Email == Member.Email);
        if (existingEmail != null)
        {
            ModelState.AddModelError("Member.Email", "Diese E-Mail-Adresse wird bereits verwendet.");
        }

        // Validate privacy consent
        if (!PrivacyConsented)
        {
            ModelState.AddModelError("PrivacyConsented", "Bitte stimmen Sie der Datenschutzerklärung zu.");
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
            // Record privacy consent on Member before creating family members
            if (PrivacyConsented)
            {
                Member.PrivacyConsentedAt = DateTime.UtcNow;
            }

            // Create all family members
            var createdMembers = await memberService.CreateFamilyAsync(Member, subMembers, Member.Email);

            // Create invitations for all family members
            foreach (var cm in createdMembers)
            {
                var invitation = await invitationService.CreateInvitationAsync(cm.Id, cm.Email);
                var inviteUrl = Url.PageLink("Activate", null, new { token = invitation.Token });

                await accountService.SendInvitationEmailAsync(
                    cm.Email,
                    $"{cm.FirstName} {cm.LastName}",
                    inviteUrl);
            }

            // Privacy consent recorded on Member before CreateAsync (see below)

            TempData["Success"] = $"{createdMembers.Count} Familienmitglieder wurden registriert.";
            return RedirectToPage("RegisterConfirmation", new { email = Member.Email });
        }
        catch (Exception ex)
        {
            var detail = ex.Message;
            if (ex.InnerException != null)
                detail += " | Inner: " + ex.InnerException.Message;
            ModelState.AddModelError(string.Empty, $"Registrierung fehlgeschlagen: {detail}");
            await LoadOptionsAsync();
            return Page();
        }
    }

    private bool TryComposeBirthDate()
    {
        if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue)
        {
            try
            {
                Member.BirthDate = new DateTime(BirthYear.Value, BirthMonth.Value, BirthDay.Value);
                return true;
            }
                catch (ArgumentOutOfRangeException)
            {
                ModelState.AddModelError("Member.BirthDate", "Bitte geben Sie ein gültiges Datum ein.");
                return false;
            }
        }

        ModelState.AddModelError("Member.BirthDate", "Bitte wählen Sie Ihr Geburtsdatum aus (Tag, Monat, Jahr).");
        return false;
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
        public int? BirthDay { get; set; }
        public int? BirthMonth { get; set; }
        public int? BirthYear { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime BirthDate
        {
            get
            {
                if (BirthDay.HasValue && BirthMonth.HasValue && BirthYear.HasValue)
                    return new DateTime(BirthYear.Value, BirthMonth.Value, BirthDay.Value);
                return DateTime.MinValue;
            }
        }
        public string Relationship { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.Today;
    }
}