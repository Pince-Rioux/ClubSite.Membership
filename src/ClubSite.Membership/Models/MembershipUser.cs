using Microsoft.AspNetCore.Identity;

namespace ClubSite.Membership.Models;

/// <summary>
/// Identity user for membership login.
/// Lives in the Membership schema alongside Member, Department, etc.
/// Each MembershipUser links to a Member record via email.
/// </summary>
public class MembershipUser : IdentityUser
{
    // Add membership-specific identity fields here if needed later.
    // For now, IdentityUser's built-in fields (Email, UserName, PasswordHash) are sufficient.
    // The link to the Member business entity is by Email (both unique).
}