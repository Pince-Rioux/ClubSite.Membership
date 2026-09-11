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
public class EditModel(
    IMemberService memberService,
    IValidator<Member> validator,
    MembershipDbContext context) : PageModel
{
    [BindProperty]
    public Member Member { get; set; } = new();

    [BindProperty]
    public int? BirthDay { get; set; }

    [BindProperty]
    public int? BirthMonth { get; set; }

    [BindProperty]
    public int? BirthYear { get; set; }

    public List<SelectListItem> FeeOptions { get; set; } = [];
    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> GuardianOptions { get; set; } = [];
    public List<SelectListItem> FamilyGroupOptions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var member = await memberService.GetByIdAsync(id);
        if (member == null)
        {
            return NotFound();
        }

        Member = member;
        if (member.BirthDate.Year > 1)
        {
            BirthDay = member.BirthDate.Day;
            BirthMonth = member.BirthDate.Month;
            BirthYear = member.BirthDate.Year;
        }
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Compose BirthDate from cross-browser selects
        if (BirthYear.HasValue && BirthMonth.HasValue && BirthDay.HasValue)
        {
            try { Member.BirthDate = new DateTime(BirthYear.Value, BirthMonth.Value, BirthDay.Value); }
            catch (ArgumentOutOfRangeException) { ModelState.AddModelError("Member.BirthDate", "Invalid date."); }
        }

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
            var updated = await memberService.UpdateAsync(Member);
            TempData["Success"] = $"Member '{updated.FirstName} {updated.LastName}' updated successfully.";
            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadOptionsAsync();
            return Page();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private async Task LoadOptionsAsync()
    {
        FeeOptions = await context.MembershipFees
            .Select(f => new SelectListItem { Value = f.Id.ToString(), Text = f.Name + " (" + f.Amount.ToString("C") + ")" })
            .ToListAsync();

        DepartmentOptions = await context.Departments
            .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
            .ToListAsync();

        GuardianOptions = await context.Members
            .Where(m => m.MembershipFee!.Category == FeeCategory.Adult && m.Status == MemberStatus.Active)
            .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.LastName + ", " + m.FirstName })
            .ToListAsync();

        FamilyGroupOptions = await context.FamilyGroups
            .Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name ?? "Family #" + g.Id })
            .ToListAsync();
    }
}