using ClubSite.Membership.Extensions;
using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Areas.Membership.Pages.Departments;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class IndexModel(MembershipDbContext context) : PageModel
{
    public List<Department> Departments { get; set; } = [];

    public async Task OnGetAsync()
    {
        Departments = await context.Departments
            .Include(d => d.Members)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }
}