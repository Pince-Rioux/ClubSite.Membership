using VereinMember.Data;
using VereinMember.Extensions;
using VereinMember.Models;
using VereinMember.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Pages.Members;

[Authorize(Policy = AuthorizationPolicies.MemberView)]
public class IndexModel(IMemberService memberService, MembershipDbContext context) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? DepartmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FeeCategory { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }
    public string? StatusFilter => Status;
    public int? DepartmentIdFilter => DepartmentId;

    public List<Member> Members { get; set; } = [];
    public List<SelectListItem> StatusOptions { get; set; } = [];
    public List<SelectListItem> DepartmentOptions { get; set; } = [];
    public List<SelectListItem> FeeCategoryOptions { get; set; } = [];

    public async Task OnGetAsync()
    {
        const int pageSize = 20;

        MemberStatus? statusFilter = Status switch
        {
            "Active" => MemberStatus.Active,
            "Passive" => MemberStatus.Passive,
            "Honorary" => MemberStatus.Honorary,
            "Former" => MemberStatus.Former,
            _ => null
        };

        Models.FeeCategory? feeCategoryFilter = FeeCategory switch
        {
            "Adult" => Models.FeeCategory.Adult,
            "Youth" => Models.FeeCategory.Youth,
            "Family" => Models.FeeCategory.Family,
            _ => null
        };

        var (items, totalCount) = await memberService.SearchAsync(SearchTerm, statusFilter, DepartmentId, feeCategoryFilter, CurrentPage, pageSize);

        Members = items;
        TotalPages = totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;

        StatusOptions = Enum.GetValues<MemberStatus>()
            .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() })
            .ToList();

        DepartmentOptions = await context.Departments
            .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
            .ToListAsync();

        FeeCategoryOptions = Enum.GetValues<Models.FeeCategory>()
            .Select(f => new SelectListItem { Value = f.ToString(), Text = f.ToString() })
            .ToList();
    }
}