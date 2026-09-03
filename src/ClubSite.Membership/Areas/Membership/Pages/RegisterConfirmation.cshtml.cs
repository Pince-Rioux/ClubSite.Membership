using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClubSite.Membership.Areas.Membership.Pages;

public class RegisterConfirmationModel : PageModel
{
    public string Email { get; set; } = string.Empty;

    public void OnGet(string email)
    {
        Email = email;
    }
}