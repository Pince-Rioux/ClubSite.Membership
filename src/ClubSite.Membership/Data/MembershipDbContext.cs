using ClubSite.Membership.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Data;

public class MembershipDbContext : DbContext
{
    public MembershipDbContext(DbContextOptions<MembershipDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<MembershipFee> MembershipFees => Set<MembershipFee>();
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Membership");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MembershipDbContext).Assembly);
    }
}