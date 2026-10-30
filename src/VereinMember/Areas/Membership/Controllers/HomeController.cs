using VereinMember.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VereinMember.Areas.Membership.Controllers;

[Area("Membership")]
[Authorize(Policy = AuthorizationPolicies.MemberView)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Members", new { area = "Membership" });
    }
}