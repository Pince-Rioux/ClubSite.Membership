using VereinMember.Data;
using VereinMember.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace VereinMember.Services;

/// <summary>
/// Background service that periodically purges personal data of former members
/// whose retention period has expired (3 years after LeftAt).
/// Runs every 24 hours.
/// </summary>
public class PurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PurgeService> _logger;

    public PurgeService(IServiceScopeFactory scopeFactory, ILogger<PurgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PurgeService started. Retention period: 3 years.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeExpiredRecordsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during purge cycle");
            }

            // Run every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeExpiredRecordsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var cutoff = DateTime.UtcNow.AddYears(-3);

        var expiredMembers = await context.Members
            .Where(m => m.Status == MemberStatus.Former
                        && m.LeftAt != null
                        && m.LeftAt < cutoff
                        && m.FirstName != "[erased]")  // skip already erased
            .ToListAsync(ct);

        if (expiredMembers.Count == 0)
        {
            _logger.LogInformation("Purge: no expired records found.");
            return;
        }

        foreach (var member in expiredMembers)
        {
            // Erase personal data
            var oldValues = JsonSerializer.Serialize(member);
            member.FirstName = "[erased]";
            member.LastName = "[erased]";
            member.Email = $"[erased-{member.Id}]@verein.de";
            member.Phone = null;
            member.BirthDate = DateTime.MinValue;
            member.Street = null;
            member.City = null;
            member.ZipCode = null;
            member.Country = null;
            member.BankIban = null;
            member.BankBic = null;
            member.Notes = null;
            member.GuardianMemberId = null;
            member.FamilyGroupId = null;
            member.DepartmentId = null;
            member.UpdatedAt = DateTime.UtcNow;

            await auditService.LogAsync(
                nameof(Member), member.Id, "Purge",
                oldValues, JsonSerializer.Serialize(member),
                nameof(Member), "system");
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation("Purge: erased {Count} expired member records.", expiredMembers.Count);
    }
}