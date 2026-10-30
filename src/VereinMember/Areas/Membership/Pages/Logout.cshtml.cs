using VereinMember.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VereinMember.Areas.Membership.Pages;

public class LogoutModel : PageModel
{
    private readonly SignInManager<MembershipUser> _signInManager;

    public LogoutModel(SignInManager<MembershipUser> signInManager)
    {
        _signInManager = signInManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await _signInManager.SignOutAsync();
        return Redirect("~/Membership/Login");
    }
}