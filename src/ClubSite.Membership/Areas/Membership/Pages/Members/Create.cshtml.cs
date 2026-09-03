using ClubSite.Membership.Data;
using ClubSite.Membership.Extensions;
using ClubSite.Membership.Models;
using ClubSite.Membership.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Areas.Membership.Pages.Members;

[Authorize(Policy = AuthorizationPolicies.MemberWrite)]
[ValidateAntiForgeryToken]
public class CreateModel(
    IMemberService memberService,
    IValidator<Member> validator,
    MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Member Member { get; set; } = new();

    /// <summary>
    /// Additional family members (only used when FeeCategory = Family).
    /// </summary>
    [BindProperty]
    public List<FamilyMemberItem> FamilyMembers { get; set; } = [];

    public List<SelectListItem> FeeOptions { get; set; } = [];
    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> GuardianOptions { get; set; } = [];
    public bool IsFamilyFee { get; set; }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var createdBy = User.Identity?.Name ?? "system";

        // Check if Family fee category
        var selectedFee = await context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == Member.MembershipFeeId);

        if (selectedFee?.Category == FeeCategory.Family)
        {
            return await HandleFamilyCreateAsync(createdBy);
        }

        // Standard (non-family) create
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

        var created = await memberService.CreateAsync(Member, createdBy);

        TempData["Success"] = $"Member '{created.FirstName} {created.LastName}' created successfully.";
        return RedirectToPage("Index");
    }

    private async Task<IActionResult> HandleFamilyCreateAsync(string createdBy)
    {
        // Collect all members: main + family sub-members (if any)
        var allFamilyMembers = new List<Member>();
        bool hasErrors = false;

        // Validate main member (skip FamilyGroupId check — it's set programmatically)
        Member.GuardianMemberId = null; // Family members don't have guardians
        var mainResult = await validator.ValidateAsync(Member, options =>
            options.IncludeRuleSets(new string[0])); // lightweight: just run all rules

        if (!mainResult.IsValid)
        {
            foreach (var error in mainResult.Errors)
            {
                // Skip FamilyGroupId error — it will be set by the service
                if (error.PropertyName == "FamilyGroupId") continue;
                ModelState.AddModelError($"Member.{error.PropertyName}", error.ErrorMessage);
                hasErrors = true;
            }
        }

        // Convert FamilyMemberItem → Member for each sub-member
        var subMembers = new List<Member>();
        for (int i = 0; i < FamilyMembers.Count; i++)
        {
            var fm = FamilyMembers[i];
            if (string.IsNullOrWhiteSpace(fm.FirstName) && string.IsNullOrWhiteSpace(fm.LastName) && string.IsNullOrWhiteSpace(fm.Email))
                continue; // skip empty rows

            var subMember = new Member
            {
                FirstName = fm.FirstName?.Trim() ?? "",
                LastName = fm.LastName?.Trim() ?? "",
                Email = fm.Email?.Trim() ?? "",
                BirthDate = fm.BirthDate,
                MembershipFeeId = Member.MembershipFeeId, // same fee category
                Street = Member.Street,
                City = Member.City,
                ZipCode = Member.ZipCode,
                Country = Member.Country,
            };

            // Validate sub-member
            var subResult = await validator.ValidateAsync(subMember, options =>
                options.IncludeRuleSets(new string[0]));

            if (!subResult.IsValid)
            {
                foreach (var error in subResult.Errors)
                {
                    ModelState.AddModelError($"FamilyMembers[{i}].{error.PropertyName}", error.ErrorMessage);
                    hasErrors = true;
                }
            }

            subMembers.Add(subMember);
        }

        if (hasErrors)
        {
            await LoadOptionsAsync();
            return Page();
        }

        // Minimum 2 total members (main + at least 1 sub)
        if (subMembers.Count < 1)
        {
            ModelState.AddModelError(string.Empty, "A family requires at least 2 members. Please add at least one additional family member.");
            await LoadOptionsAsync();
            return Page();
        }

        try
        {
            var createdMembers = await memberService.CreateFamilyAsync(Member, subMembers, createdBy);
            TempData["Success"] = $"{createdMembers.Count} family members created successfully.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
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

        DepartmentOptions = await context.Departments
            .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
            .ToListAsync();

        GuardianOptions = await context.Members
            .Where(m => m.MembershipFee!.Category == FeeCategory.Adult && m.Status == MemberStatus.Active)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.LastName + ", " + m.FirstName })
            .ToListAsync();
    }

    public class FamilyMemberItem
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Relationship { get; set; } = "";
    }
}