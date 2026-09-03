using ClubSite.Membership.Data;
using ClubSite.Membership.Services;
using ClubSite.Membership.Services.Validators;
using ClubSite.Membership.Services.Encryption;
using ClubSite.Membership.Data.EntityConfigurations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ClubSite.Membership.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;

namespace ClubSite.Membership.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClubSiteMembership(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Use ClubMembershipDb connection string, fall back to shared VolleyballClub
        var connStr = configuration.GetConnectionString("ClubMembershipDb")
                      ?? configuration.GetConnectionString("VolleyballClub")
                      ?? throw new InvalidOperationException("No valid connection string found. Configure 'ClubMembershipDb' or 'VolleyballClub'.");

        services.AddDbContext<MembershipDbContext>(options =>
            options.UseSqlServer(connStr));

        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IValidator<Member>, MemberValidator>();

        // Add AES-256-GCM encryption for sensitive personal data
        var key = configuration["MembershipEncryption:Key"]
                  ?? Environment.GetEnvironmentVariable("MEMBERSHIP_ENCRYPTION_KEY")
                  ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var encryptionService = new AesEncryptionService(key);
        services.AddSingleton<IEncryptionService>(encryptionService);

        // Initialize static encryption reference for EF Core value converters
        MembershipEncryption.Initialize(encryptionService);

        // Background service that purges expired personal data every 24 hours
        services.AddHostedService<PurgeService>();

        services.AddMembershipAuthorization();

        return services;
    }
}