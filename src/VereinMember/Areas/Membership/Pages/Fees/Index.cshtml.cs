using VereinMember.Extensions;
using VereinMember.Data;
using VereinMember.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Pages.Fees;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class IndexModel(MembershipDbContext context) : PageModel
{
    public List<MembershipFee> Fees { get; set; } = [];

    public async Task OnGetAsync()
    {
        Fees = await context.MembershipFees
            .Include(f => f.Members)
            .AsNoTracking()
            .OrderBy(f => f.Category)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }
}