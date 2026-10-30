using VereinMember.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace VereinMember.Areas.Membership.Pages;

public class LoginModel : PageModel
{
    private readonly SignInManager<MembershipUser> _signInManager;
    private readonly UserManager<MembershipUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<MembershipUser> signInManager,
        UserManager<MembershipUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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

        // Sign out any existing session first
        await _signInManager.SignOutAsync();

        // Find the user by email
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Member login failed for {Email}: user not found", email);
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "E-Mail und/oder Passwort sind falsch.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, true, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Member login failed for {Email}: {Reason}",
                email, result.IsLockedOut ? "locked out" : "wrong password");
            ModelState.Clear();
            ModelState.AddModelError(string.Empty, "E-Mail und/oder Passwort sind falsch.");
            return Page();
        }

        _logger.LogInformation("Member login successful for {Email}", email);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        // Role-aware default landing page:
        // Admin → member management, everyone else → their own profile.
        // Note: User.IsInRole() would reflect the previous request's principal,
        // so we check the role via UserManager instead.
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return Redirect("~/Membership/Members");
        }

        return Redirect("~/Membership/Profile");
    }
}