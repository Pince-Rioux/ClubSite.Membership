using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Piranha.Manager.LocalAuth;

namespace ClubSite.Membership.Areas.Membership.Pages;

public class LoginModel : PageModel
{
    private readonly ISecurity _security;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(ISecurity security, ILogger<LoginModel> logger)
    {
        _security = security;
        _logger = logger;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    [BindProperty]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        var email = Request.Form["email"].ToString();
        var password = Request.Form["password"].ToString();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError(string.Empty, "Bitte geben Sie E-Mail und Passwort ein.");
            return Page();
        }

        await _security.SignOut(HttpContext);

        var result = await _security.SignIn(HttpContext, email, password);

        if (result != LoginResult.Succeeded)
        {
            _logger.LogWarning("Member login failed for {Email}", email);
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "E-Mail und/oder Passwort sind falsch.");
            return Page();
        }

        _logger.LogInformation("Member login successful for {Email}", email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect("~/Membership/Members");
    }
}