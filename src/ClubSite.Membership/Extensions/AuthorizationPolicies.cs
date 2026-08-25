using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ClubSite.Membership.Extensions;

public static class AuthorizationPolicies
{
    public const string MemberView = "MemberView";
    public const string MemberWrite = "MemberWrite";
    public const string MemberDelete = "MemberDelete";
    public const string AdminOnly = "AdminOnly";

    public static IServiceCollection AddMembershipAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(MemberView, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.IsInRole("Editor") ||
                    ctx.User.IsInRole("Viewer")));

            options.AddPolicy(MemberWrite, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.IsInRole("Editor")));

            options.AddPolicy(MemberDelete, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(AdminOnly, policy =>
                policy.RequireRole("Admin"));
        });

        return services;
    }
}