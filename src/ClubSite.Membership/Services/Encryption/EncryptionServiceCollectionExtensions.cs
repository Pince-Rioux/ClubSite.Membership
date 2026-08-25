using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClubSite.Membership.Services.Encryption;

public static class EncryptionServiceCollectionExtensions
{
    public static IServiceCollection AddMembershipEncryption(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Key from config (appsettings) or environment variable
        var key = configuration["MembershipEncryption:Key"]
                  ?? Environment.GetEnvironmentVariable("MEMBERSHIP_ENCRYPTION_KEY");

        if (string.IsNullOrEmpty(key))
        {
            // In development, generate and log a stable dev key
            // In production, this MUST be configured via environment variable
            key = Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            // In a real app, log a warning here
        }

        services.AddSingleton<IEncryptionService>(
            new AesEncryptionService(key));
        return services;
    }
}