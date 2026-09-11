using ClubSite.Membership.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Data;

public class MembershipDbContext : IdentityDbContext<MembershipUser>
{
    public MembershipDbContext(DbContextOptions<MembershipDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<MembershipFee> MembershipFees => Set<MembershipFee>();
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Set default schema to Membership for ALL tables (including Identity tables)
        modelBuilder.HasDefaultSchema("Membership");

        // Apply entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MembershipDbContext).Assembly);

        // Configure Identity entities to use Membership schema
        // This is needed because Identity's base OnModelCreating adds tables in 'dbo' schema
        // and we want them in 'Membership' schema alongside our business tables.
        ConfigureIdentitySchema(modelBuilder);

        // Call base to let Identity configure its entities
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureIdentitySchema(ModelBuilder builder)
    {
        var schema = "Membership";

        builder.Entity<MembershipUser>(entity =>
        {
            entity.ToTable("AspNetUsers", schema);
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("AspNetRoles", schema);
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("AspNetUserRoles", schema);
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("AspNetUserClaims", schema);
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("AspNetUserLogins", schema);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("AspNetUserTokens", schema);
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("AspNetRoleClaims", schema);
        });
    }
}