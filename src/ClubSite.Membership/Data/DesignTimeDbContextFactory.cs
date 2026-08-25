using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClubSite.Membership.Data;

/// <summary>
/// Design-time factory for EF Core migrations. Uses 'ClubMembershipDb' connection string
/// from dev credentials, or falls back to the MEMBERSHIP_CONNECTION_STRING env var.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MembershipDbContext>
{
    public MembershipDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MembershipDbContext>();

        // Try environment variable first (CI-friendly)
        var connStr = Environment.GetEnvironmentVariable("MEMBERSHIP_CONNECTION_STRING");

        // Fall back to default (matches dev credentials)
        connStr ??= "Server=127.0.0.1,1433;Database=ClubSite_Membership;User Id=sa;Password=ClubSiteSql2026!;Integrated Security=false;Connection Timeout=30;Pooling=true;MultipleActiveResultSets=true;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connStr);
        return new MembershipDbContext(optionsBuilder.Options);
    }
}