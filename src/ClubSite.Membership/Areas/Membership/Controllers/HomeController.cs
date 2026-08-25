using ClubSite.Membership.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubSite.Membership.Areas.Membership.Controllers;

[Area("Membership")]
[Authorize(Policy = AuthorizationPolicies.MemberView)]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Members", new { area = "Membership" });
    }
}