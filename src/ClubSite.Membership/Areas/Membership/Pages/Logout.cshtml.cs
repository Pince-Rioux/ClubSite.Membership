using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Piranha.Manager.LocalAuth;

namespace ClubSite.Membership.Areas.Membership.Pages;

public class LogoutModel : PageModel
{
    private readonly ISecurity _security;

    public LogoutModel(ISecurity security)
    {
        _security = security;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _security.SignOut(HttpContext);
        return Redirect("~/Membership/Login");
    }
}