using ClubSite.Membership.Tests.Fixtures;

namespace ClubSite.Membership.Tests.Integration;

/// <summary>
/// Integration tests using EF Core InMemory provider.
/// Database-level constraints (unique indexes, foreign keys) are enforced by SQL Server only.
/// These tests verify the service-layer logic, data persistence, and relationship mappings.
/// </summary>
public sealed class DbContextIntegrationTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public DbContextIntegrationTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    // ----------------------------------------------------------------------- //
    //  Basic CRUD
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task CanCreateAndQueryDepartment()
    {
        await using var context = _dbFixture.CreateContext();

        var dept = new Department { Name = "Hockey", Description = "Ice Hockey" };
        context.Departments.Add(dept);
        await context.SaveChangesAsync();

        dept.Id.Should().BeGreaterThan(0);

        var fromDb = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Hockey");
        fromDb.Should().NotBeNull();
        fromDb!.Description.Should().Be("Ice Hockey");
    }

    [Fact]
    public async Task CanCreateAndQueryMembershipFee()
    {
        await using var context = _dbFixture.CreateContext();

        var fee = new MembershipFee
        {
            Name = "Student Fee",
            Amount = 30m,
            Interval = FeeInterval.Monthly,
            Category = FeeCategory.Youth,
            IsDefault = false
        };
        context.MembershipFees.Add(fee);
        await context.SaveChangesAsync();

        var fromDb = await context.MembershipFees
            .FirstOrDefaultAsync(f => f.Category == FeeCategory.Youth && f.IsDefault == false);
        fromDb.Should().NotBeNull();
        fromDb!.Amount.Should().Be(30m);
        fromDb.Interval.Should().Be(FeeInterval.Monthly);
    }

    [Fact]
    public async Task CanCreateMemberWithDepartmentFeeAndGuardian()
    {
        await using var context = _dbFixture.CreateContext();

        var dept = new Department { Name = "Basketball" };
        var adultFee = new MembershipFee
        {
            Name = "Adult",
            Amount = 50m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Adult,
            IsDefault = true
        };
        var youthFee = new MembershipFee
        {
            Name = "Youth",
            Amount = 25m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Youth,
            IsDefault = false
        };
        context.Departments.Add(dept);
        context.MembershipFees.AddRange(adultFee, youthFee);
        await context.SaveChangesAsync();

        var guardian = new Member
        {
            FirstName = "Parent",
            LastName = "One",
            Email = "parent@example.com",
            BirthDate = new DateTime(1980, 5, 15),
            MembershipFeeId = adultFee.Id,
            DepartmentId = dept.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow.AddDays(-100),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(guardian);
        await context.SaveChangesAsync();

        var youth = new Member
        {
            FirstName = "Child",
            LastName = "One",
            Email = "child@example.com",
            BirthDate = new DateTime(2012, 3, 10),
            MembershipFeeId = youthFee.Id,
            DepartmentId = dept.Id,
            GuardianMemberId = guardian.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(youth);
        await context.SaveChangesAsync();

        // Verify relationships were saved
        var fromDb = await context.Members
            .Include(m => m.Department)
            .Include(m => m.MembershipFee)
            .Include(m => m.GuardianMember)
            .FirstAsync(m => m.Id == youth.Id);

        fromDb.Department.Should().NotBeNull();
        fromDb.Department!.Name.Should().Be("Basketball");

        fromDb.MembershipFee.Should().NotBeNull();
        fromDb.MembershipFee!.Category.Should().Be(FeeCategory.Youth);

        fromDb.GuardianMember.Should().NotBeNull();
        fromDb.GuardianMember!.Email.Should().Be("parent@example.com");
    }

    [Fact]
    public async Task CanCreateMemberWithFamilyGroup()
    {
        await using var context = _dbFixture.CreateContext();

        var familyFee = new MembershipFee
        {
            Name = "Family",
            Amount = 80m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Family,
            IsDefault = false
        };
        context.MembershipFees.Add(familyFee);

        var group = new FamilyGroup { Name = "Johnson Family", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var member1 = new Member
        {
            FirstName = "John",
            LastName = "Johnson",
            Email = "john@family.com",
            BirthDate = new DateTime(1980, 1, 1),
            MembershipFeeId = familyFee.Id,
            FamilyGroupId = group.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        var member2 = new Member
        {
            FirstName = "Jane",
            LastName = "Johnson",
            Email = "jane@family.com",
            BirthDate = new DateTime(1982, 1, 1),
            MembershipFeeId = familyFee.Id,
            FamilyGroupId = group.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.AddRange(member1, member2);
        await context.SaveChangesAsync();

        var fromDb = await context.FamilyGroups
            .Include(g => g.Members)
            .FirstAsync(g => g.Id == group.Id);

        fromDb.Members.Should().HaveCount(2);
        fromDb.Members.All(m => m.FamilyGroupId == group.Id).Should().BeTrue();
    }

    // ----------------------------------------------------------------------- //
    //  Soft-delete cascade (service-level)
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task SoftDeleteGuardianWithActiveDependent_BlockedByService()
    {
        // This is enforced by the service, not the database
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = new MembershipFee
        {
            Name = "Adult", Amount = 50m, Interval = FeeInterval.Yearly,
            Category = FeeCategory.Adult, IsDefault = true
        };
        var youthFee = new MembershipFee
        {
            Name = "Youth", Amount = 25m, Interval = FeeInterval.Yearly,
            Category = FeeCategory.Youth, IsDefault = false
        };
        context.MembershipFees.AddRange(adultFee, youthFee);

        var guardian = new Member
        {
            FirstName = "G", LastName = "Uardian", Email = "g@example.com",
            BirthDate = new DateTime(1980, 1, 1),
            MembershipFeeId = adultFee.Id, Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow, CreatedBy = "admin"
        };
        context.Members.Add(guardian);
        await context.SaveChangesAsync();

        var dependent = new Member
        {
            FirstName = "D", LastName = "Ependent", Email = "d@example.com",
            BirthDate = new DateTime(2010, 1, 1),
            MembershipFeeId = youthFee.Id, GuardianMemberId = guardian.Id,
            Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(dependent);
        await context.SaveChangesAsync();

        var act = () => service.SoftDeleteAsync(guardian.Id, "admin");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*guardian*active*Youth*");
    }

    // ----------------------------------------------------------------------- //
    //  FamilyGroup min-2 enforced (service-level)
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task FamilyGroupMin2Enforced_WhenRemovingMember()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = new MembershipFee
        {
            Name = "Family", Amount = 80m, Interval = FeeInterval.Yearly,
            Category = FeeCategory.Family, IsDefault = false
        };
        context.MembershipFees.Add(familyFee);

        var group = new FamilyGroup { Name = "Min2", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var m = new Member
        {
            FirstName = "Only", LastName = "Member", Email = "only@example.com",
            BirthDate = new DateTime(1980, 1, 1),
            MembershipFeeId = familyFee.Id, FamilyGroupId = group.Id,
            Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(m);
        await context.SaveChangesAsync();

        var act = () => service.RemoveMemberFromFamilyGroupAsync(m.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least 2*");
    }

    // ----------------------------------------------------------------------- //
    //  Audit log entries
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task AuditLogEntriesArePersisted()
    {
        await using var context = _dbFixture.CreateContext();
        var auditService = new AuditService(context);

        await auditService.LogAsync("Member", 1, "Created", null, "{}", "FirstName,LastName", "admin");

        var entries = await context.AuditLog.ToListAsync();
        entries.Should().HaveCount(1);
        entries[0].Action.Should().Be("Created");
        entries[0].EntityName.Should().Be("Member");
        entries[0].EntityId.Should().Be(1);
        entries[0].PerformedBy.Should().Be("admin");
        entries[0].PerformedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}