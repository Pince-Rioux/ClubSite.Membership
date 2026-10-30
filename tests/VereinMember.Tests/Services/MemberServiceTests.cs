using VereinMember.Tests.Fixtures;

namespace VereinMember.Services.Tests;

public sealed class MemberServiceTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _dbFixture;

    public MemberServiceTests(DbFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    private static MembershipFee CreateAdultFee(int id = 1)
    {
        return new MembershipFee
        {
            Id = id,
            Name = "Adult Fee",
            Amount = 50m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Adult,
            IsDefault = true
        };
    }

    private static MembershipFee CreateYouthFee(int id = 2)
    {
        return new MembershipFee
        {
            Id = id,
            Name = "Youth Fee",
            Amount = 25m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Youth,
            IsDefault = false
        };
    }

    private static MembershipFee CreateFamilyFee(int id = 3)
    {
        return new MembershipFee
        {
            Id = id,
            Name = "Family Fee",
            Amount = 80m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Family,
            IsDefault = false
        };
    }

    private static Department CreateDepartment(int id = 1)
    {
        return new Department
        {
            Id = id,
            Name = "Soccer",
            Description = "Soccer Department"
        };
    }

    private static Member CreateAdultMember(int id, string email, MembershipFee fee, Department? dept = null, int? guardianId = null)
    {
        return new Member
        {
            Id = id,
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            BirthDate = new DateTime(1990, 1, 1),
            Status = MemberStatus.Active,
            MembershipFeeId = fee.Id,
            MembershipFee = fee,
            DepartmentId = dept?.Id,
            Department = dept,
            JoinedAt = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30),
            CreatedBy = "test-user"
        };
    }

    // ----------------------------------------------------------------------- //
    //  CreateAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task CreateAdultMember_ClearsGuardianAndFamilyFields()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@example.com",
            BirthDate = new DateTime(1992, 5, 5),
            MembershipFeeId = fee.Id,
            GuardianMemberId = 99,   // should be cleared
            FamilyGroupId = 999      // should be cleared
        };

        var result = await service.CreateAsync(member, "admin");

        result.GuardianMemberId.Should().BeNull();
        result.FamilyGroupId.Should().BeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateYouthMemberWithoutGuardian_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var youthFee = CreateYouthFee();
        context.MembershipFees.Add(youthFee);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Young",
            LastName = "Person",
            Email = "young@example.com",
            BirthDate = new DateTime(2010, 6, 1),
            MembershipFeeId = youthFee.Id
            // No guardian set
        };

        var act = () => service.CreateAsync(member, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Youth member must have a guardian.");
    }

    [Fact]
    public async Task CreateYouthMemberWithNonAdultGuardian_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var youthFee = CreateYouthFee();
        var youthFee2 = CreateYouthFee(22);
        context.MembershipFees.AddRange(youthFee, youthFee2);
        await context.SaveChangesAsync();

        // Guardian is also a Youth member -> invalid
        var guardian = new Member
        {
            FirstName = "Teen",
            LastName = "Guardian",
            Email = "teen-guardian@example.com",
            BirthDate = new DateTime(2008, 3, 1),
            MembershipFeeId = youthFee2.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(guardian);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Child",
            LastName = "Test",
            Email = "child@example.com",
            BirthDate = new DateTime(2015, 1, 1),
            MembershipFeeId = youthFee.Id,
            GuardianMemberId = guardian.Id
        };

        var act = () => service.CreateAsync(member, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Guardian must be an Adult member.");
    }

    [Fact]
    public async Task CreateFamilyMemberWithoutFamilyGroup_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = CreateFamilyFee();
        context.MembershipFees.Add(familyFee);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Family",
            LastName = "Member",
            Email = "family@example.com",
            BirthDate = new DateTime(1985, 1, 1),
            MembershipFeeId = familyFee.Id
            // No family group
        };

        var act = () => service.CreateAsync(member, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Family member must belong to a family group.");
    }

    [Fact]
    public async Task CreateValidYouthMember_Succeeds()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var youthFee = CreateYouthFee();
        context.MembershipFees.AddRange(adultFee, youthFee);

        var adult = new Member
        {
            FirstName = "Adult",
            LastName = "Guardian",
            Email = "adult-guardian@example.com",
            BirthDate = new DateTime(1990, 1, 1),
            MembershipFeeId = adultFee.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(adult);
        await context.SaveChangesAsync();

        var youth = new Member
        {
            FirstName = "Youth",
            LastName = "Dependent",
            Email = "youth@example.com",
            BirthDate = new DateTime(2010, 5, 5),
            MembershipFeeId = youthFee.Id,
            GuardianMemberId = adult.Id
        };

        var result = await service.CreateAsync(youth, "admin");

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.GuardianMemberId.Should().Be(adult.Id);
    }

    [Fact]
    public async Task CreateValidFamilyMember_Succeeds()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = CreateFamilyFee();
        context.MembershipFees.Add(familyFee);

        var group = new FamilyGroup { Name = "Smith Family", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Family",
            LastName = "Smith",
            Email = "family.smith@example.com",
            BirthDate = new DateTime(1980, 3, 15),
            MembershipFeeId = familyFee.Id,
            FamilyGroupId = group.Id
        };

        var result = await service.CreateAsync(member, "admin");

        result.Should().NotBeNull();
        result.FamilyGroupId.Should().Be(group.Id);
    }

    [Fact]
    public async Task CreateMember_AuditLogEntryCreated()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Audit",
            LastName = "Test",
            Email = "audit@example.com",
            BirthDate = new DateTime(1995, 1, 1),
            MembershipFeeId = fee.Id
        };

        await service.CreateAsync(member, "auditor");

        auditMock.Verify(a => a.LogAsync(
            "Member",
            It.IsAny<int>(),
            "Created",
            null,
            It.IsAny<string>(),
            "Member",
            "auditor"
        ), Times.Once);
    }

    // ----------------------------------------------------------------------- //
    //  UpdateAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task UpdateExistingMember_Succeeds()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        var member = CreateAdultMember(0, "update@example.com", fee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var createdId = member.Id;
        context.ChangeTracker.Clear(); // avoid tracking conflict

        var update = new Member
        {
            Id = createdId,
            FirstName = "Updated",
            LastName = "Name",
            Email = "updated@example.com",
            BirthDate = new DateTime(1990, 1, 1),
            MembershipFeeId = fee.Id,
            CreatedBy = "test-user"
        };

        var result = await service.UpdateAsync(update);

        result.FirstName.Should().Be("Updated");
        result.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task UpdateNonExistingMember_ThrowsKeyNotFoundException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        await context.SaveChangesAsync();

        var member = new Member
        {
            Id = 9999,
            FirstName = "Ghost",
            LastName = "Member",
            Email = "ghost@example.com",
            BirthDate = new DateTime(2000, 1, 1),
            MembershipFeeId = fee.Id,
            CreatedBy = "test"
        };

        var act = () => service.UpdateAsync(member);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*9999*");
    }

    [Fact]
    public async Task UpdateToYouthWithoutGuardian_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var youthFee = CreateYouthFee();
        context.MembershipFees.AddRange(adultFee, youthFee);

        var member = CreateAdultMember(0, "change@example.com", adultFee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var update = new Member
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            BirthDate = member.BirthDate,
            MembershipFeeId = youthFee.Id, // switching to Youth
            CreatedBy = "test"
            // No guardian
        };

        var act = () => service.UpdateAsync(update);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Youth member must have a guardian.");
    }

    [Fact]
    public async Task UpdateToFamilyWithoutGroup_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var familyFee = CreateFamilyFee();
        context.MembershipFees.AddRange(adultFee, familyFee);

        var member = CreateAdultMember(0, "switch@example.com", adultFee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var update = new Member
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            BirthDate = member.BirthDate,
            MembershipFeeId = familyFee.Id, // switching to Family
            CreatedBy = "test"
            // No family group
        };

        var act = () => service.UpdateAsync(update);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Family member must belong to a family group.");
    }

    // ----------------------------------------------------------------------- //
    //  SoftDeleteAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task SoftDeleteExistingMember_SetsFormerAndLeftAt()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        var member = CreateAdultMember(0, "delete@example.com", fee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var result = await service.SoftDeleteAsync(member.Id, "admin");

        result.Should().BeTrue();

        var reloaded = await context.Members.FindAsync(member.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(MemberStatus.Former);
        reloaded.LeftAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteMemberWhoIsGuardian_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var youthFee = CreateYouthFee();
        context.MembershipFees.AddRange(adultFee, youthFee);

        var guardian = CreateAdultMember(0, "guardian@example.com", adultFee);
        context.Members.Add(guardian);
        await context.SaveChangesAsync();

        var youth = new Member
        {
            FirstName = "Kid",
            LastName = "Dependent",
            Email = "kid@example.com",
            BirthDate = new DateTime(2012, 1, 1),
            MembershipFeeId = youthFee.Id,
            GuardianMemberId = guardian.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.Add(youth);
        await context.SaveChangesAsync();

        var act = () => service.SoftDeleteAsync(guardian.Id, "admin");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*guardian*active*Youth*");
    }

    [Fact]
    public async Task SoftDeleteMemberInFamilyGroupDroppingBelow2_ThrowsException()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = CreateFamilyFee();
        context.MembershipFees.Add(familyFee);

        var group = new FamilyGroup { Name = "Duo", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        // Add 2 members to the family group
        var m1 = new Member
        {
            FirstName = "One",
            LastName = "Family",
            Email = "one@example.com",
            BirthDate = new DateTime(1980, 1, 1),
            MembershipFeeId = familyFee.Id,
            FamilyGroupId = group.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        var m2 = new Member
        {
            FirstName = "Two",
            LastName = "Family",
            Email = "two@example.com",
            BirthDate = new DateTime(1982, 1, 1),
            MembershipFeeId = familyFee.Id,
            FamilyGroupId = group.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.AddRange(m1, m2);
        await context.SaveChangesAsync();

        // Soft-deleting m1 would leave only 1 active member -> throws
        var act = () => service.SoftDeleteAsync(m1.Id, "admin");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*fewer than 2*");
    }

    [Fact]
    public async Task SoftDeleteNonExistingMember_ReturnsFalse()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var result = await service.SoftDeleteAsync(9999, "admin");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteMember_AuditLogEntryCreated()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        var member = CreateAdultMember(0, "audit-delete@example.com", fee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        await service.SoftDeleteAsync(member.Id, "deleter");

        auditMock.Verify(a => a.LogAsync(
            "Member",
            member.Id,
            "SoftDeleted",
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            "deleter"
        ), Times.Once);
    }

    // ----------------------------------------------------------------------- //
    //  SearchAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task SearchWithNoFilters_ReturnsAllMembers()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        context.Members.AddRange(
            CreateAdultMember(0, "a@example.com", fee),
            CreateAdultMember(0, "b@example.com", fee),
            CreateAdultMember(0, "c@example.com", fee)
        );
        await context.SaveChangesAsync();

        var (items, total) = await service.SearchAsync(null, null, null, null, 1, 10);

        items.Should().HaveCount(3);
        total.Should().Be(3);
    }

    [Fact]
    public async Task SearchBySearchTerm_FiltersByNameAndEmail()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        context.Members.AddRange(
            CreateAdultMember(0, "alice@example.com", fee),
            CreateAdultMember(0, "bob@example.com", fee)
        );
        await context.SaveChangesAsync();

        // Change names for meaningful search
        var all = context.Members.ToList();
        all[0].FirstName = "Alice";
        all[0].LastName = "Anderson";
        all[1].FirstName = "Bob";
        all[1].LastName = "Builder";
        await context.SaveChangesAsync();

        var (items, total) = await service.SearchAsync("Alice", null, null, null, 1, 10);

        items.Should().HaveCount(1);
        items[0].FirstName.Should().Be("Alice");
        total.Should().Be(1);
    }

    [Fact]
    public async Task SearchByStatus_FiltersCorrectly()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        var active = CreateAdultMember(0, "active@example.com", fee);
        var former = CreateAdultMember(0, "former@example.com", fee);
        former.Status = MemberStatus.Former;
        context.Members.AddRange(active, former);
        await context.SaveChangesAsync();

        var (items, total) = await service.SearchAsync(null, MemberStatus.Former, null, null, 1, 10);

        items.Should().HaveCount(1);
        items[0].Email.Should().Be("former@example.com");
        total.Should().Be(1);
    }

    [Fact]
    public async Task SearchByDepartment_FiltersCorrectly()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        var dept1 = new Department { Id = 10, Name = "Tennis" };
        var dept2 = new Department { Id = 20, Name = "Swimming" };
        context.MembershipFees.Add(fee);
        context.Departments.AddRange(dept1, dept2);
        context.Members.AddRange(
            CreateAdultMember(0, "tennis@example.com", fee, dept1),
            CreateAdultMember(0, "swim@example.com", fee, dept2)
        );
        await context.SaveChangesAsync();

        var (items, total) = await service.SearchAsync(null, null, dept1.Id, null, 1, 10);

        items.Should().HaveCount(1);
        items[0].Email.Should().Be("tennis@example.com");
        total.Should().Be(1);
    }

    [Fact]
    public async Task SearchByFeeCategory_FiltersCorrectly()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var youthFee = CreateYouthFee();
        context.MembershipFees.AddRange(adultFee, youthFee);

        context.Members.AddRange(
            CreateAdultMember(0, "adult@example.com", adultFee),
            CreateAdultMember(0, "youth@example.com", youthFee)
        );
        await context.SaveChangesAsync();

        var (items, total) = await service.SearchAsync(null, null, null, FeeCategory.Youth, 1, 10);

        items.Should().HaveCount(1);
        items[0].Email.Should().Be("youth@example.com");
        total.Should().Be(1);
    }

    [Fact]
    public async Task SearchPaginatesCorrectly()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        for (int i = 0; i < 5; i++)
        {
            context.Members.Add(CreateAdultMember(0, $"page{i}@example.com", fee));
        }
        await context.SaveChangesAsync();

        var (page1, total1) = await service.SearchAsync(null, null, null, null, 1, 2);
        var (page2, total2) = await service.SearchAsync(null, null, null, null, 3, 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(1); // 3rd page of 2 = 1 item
        total1.Should().Be(5);
        total2.Should().Be(5);
    }

    // ----------------------------------------------------------------------- //
    //  GetGuardianCandidatesAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task GetGuardianCandidates_ReturnsOnlyAdultActiveMembers()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var youthFee = CreateYouthFee();
        var familyFee = CreateFamilyFee();
        context.MembershipFees.AddRange(adultFee, youthFee, familyFee);

        var adult1 = CreateAdultMember(0, "adult1@example.com", adultFee);
        var adultFormer = CreateAdultMember(0, "adult-former@example.com", adultFee);
        adultFormer.Status = MemberStatus.Former;
        var youth = CreateAdultMember(0, "youth-cand@example.com", youthFee);
        var family = CreateAdultMember(0, "family-cand@example.com", familyFee);

        context.Members.AddRange(adult1, adultFormer, youth, family);
        await context.SaveChangesAsync();

        var candidates = await service.GetGuardianCandidatesAsync();

        // Only adult1 is Adult + Active
        candidates.Should().HaveCount(1);
        candidates[0].Email.Should().Be("adult1@example.com");
    }

    [Fact]
    public async Task GetGuardianCandidates_ReturnsEmptyWhenNone()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var candidates = await service.GetGuardianCandidatesAsync();

        candidates.Should().BeEmpty();
    }

    // ----------------------------------------------------------------------- //
    //  GetDependentsAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task GetDependents_ReturnsMembersLinkedToGuardian()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        var youthFee = CreateYouthFee();
        context.MembershipFees.AddRange(adultFee, youthFee);

        var guardian = CreateAdultMember(0, "guardian@example.com", adultFee);
        context.Members.Add(guardian);
        await context.SaveChangesAsync();

        var youth1 = new Member
        {
            FirstName = "Dep1",
            LastName = "End",
            Email = "dep1@example.com",
            BirthDate = new DateTime(2010, 1, 1),
            MembershipFeeId = youthFee.Id,
            GuardianMemberId = guardian.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        var youth2 = new Member
        {
            FirstName = "Dep2",
            LastName = "End",
            Email = "dep2@example.com",
            BirthDate = new DateTime(2012, 1, 1),
            MembershipFeeId = youthFee.Id,
            GuardianMemberId = guardian.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        context.Members.AddRange(youth1, youth2);
        await context.SaveChangesAsync();

        var dependents = await service.GetDependentsAsync(guardian.Id);

        dependents.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDependents_ReturnsEmptyWhenNoDependents()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        var member = CreateAdultMember(0, "alone@example.com", fee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var dependents = await service.GetDependentsAsync(member.Id);

        dependents.Should().BeEmpty();
    }

    // ----------------------------------------------------------------------- //
    //  Family group methods
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task GetAllFamilyGroups_ReturnsAllGroups()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        context.FamilyGroups.AddRange(
            new FamilyGroup { Name = "Alpha", CreatedAt = DateTime.UtcNow },
            new FamilyGroup { Name = "Beta", CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var groups = await service.GetAllFamilyGroupsAsync();

        groups.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateFamilyGroup_CreatesAndReturns()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var group = await service.CreateFamilyGroupAsync("New Group", "admin");

        group.Should().NotBeNull();
        group.Id.Should().BeGreaterThan(0);
        group.Name.Should().Be("New Group");

        var fromDb = await context.FamilyGroups.FindAsync(group.Id);
        fromDb.Should().NotBeNull();
    }

    [Fact]
    public async Task AddMemberToFamilyGroup_WhenMemberNotFamilyCategory_Throws()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        context.MembershipFees.Add(adultFee);
        var group = new FamilyGroup { Name = "Test", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        var member = CreateAdultMember(0, "adult@example.com", adultFee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var act = () => service.AddMemberToFamilyGroupAsync(member.Id, group.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Family-category*");
    }

    [Fact]
    public async Task RemoveMemberFromFamilyGroup_WhenOnlyOneWouldRemain_Throws()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = CreateFamilyFee();
        context.MembershipFees.Add(familyFee);
        var group = new FamilyGroup { Name = "Duo", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var m1 = new Member
        {
            FirstName = "A", LastName = "B", Email = "a@example.com",
            BirthDate = new DateTime(1980, 1, 1),
            MembershipFeeId = familyFee.Id, FamilyGroupId = group.Id,
            Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "admin"
        };
        var m2 = new Member
        {
            FirstName = "C", LastName = "D", Email = "c@example.com",
            BirthDate = new DateTime(1982, 1, 1),
            MembershipFeeId = familyFee.Id, FamilyGroupId = group.Id,
            Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "admin"
        };
        context.Members.AddRange(m1, m2);
        await context.SaveChangesAsync();

        // Removing m1 would leave only 1 active -> throws
        var act = () => service.RemoveMemberFromFamilyGroupAsync(m1.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*at least 2*");
    }

    [Fact]
    public async Task RemoveMemberFromFamilyGroup_NotInGroup_Throws()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var adultFee = CreateAdultFee();
        context.MembershipFees.Add(adultFee);
        var member = CreateAdultMember(0, "nogroup@example.com", adultFee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var act = () => service.RemoveMemberFromFamilyGroupAsync(member.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not assigned to any family group*");
    }

    [Fact]
    public async Task DeleteFamilyGroup_WithActiveMembers_Throws()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = CreateFamilyFee();
        context.MembershipFees.Add(familyFee);
        var group = new FamilyGroup { Name = "Occupied", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Active", LastName = "Member", Email = "active@example.com",
            BirthDate = new DateTime(1980, 1, 1),
            MembershipFeeId = familyFee.Id, FamilyGroupId = group.Id,
            Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "admin"
        };
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var act = () => service.DeleteFamilyGroupAsync(group.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active members*");
    }

    [Fact]
    public async Task DeleteFamilyGroup_WithNoActiveMembers_Succeeds()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var group = new FamilyGroup { Name = "Empty", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var result = await service.DeleteFamilyGroupAsync(group.Id);

        result.Should().BeTrue();
        var fromDb = await context.FamilyGroups.FindAsync(group.Id);
        fromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFamilyGroup_NonExisting_ReturnsFalse()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var result = await service.DeleteFamilyGroupAsync(9999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetFamilyGroupById_ReturnsGroupWithMembers()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var familyFee = CreateFamilyFee();
        context.MembershipFees.Add(familyFee);
        var group = new FamilyGroup { Name = "Test Group", CreatedAt = DateTime.UtcNow };
        context.FamilyGroups.Add(group);
        await context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Fam", LastName = "Member", Email = "fam@example.com",
            BirthDate = new DateTime(1985, 1, 1),
            MembershipFeeId = familyFee.Id, FamilyGroupId = group.Id,
            Status = MemberStatus.Active, JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = "admin"
        };
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var result = await service.GetFamilyGroupByIdAsync(group.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Group");
        result.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFamilyGroupById_NonExisting_ReturnsNull()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var result = await service.GetFamilyGroupByIdAsync(9999);

        result.Should().BeNull();
    }

    // ----------------------------------------------------------------------- //
    //  GetByIdAsync / GetAllAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task GetById_ReturnsMemberWithIncludes()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);
        var member = CreateAdultMember(0, "byid@example.com", fee);
        context.Members.Add(member);
        await context.SaveChangesAsync();

        var result = await service.GetByIdAsync(member.Id);

        result.Should().NotBeNull();
        result!.MembershipFee.Should().NotBeNull();
        result.MembershipFee!.Name.Should().Be("Adult Fee");
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNull()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var result = await service.GetByIdAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsOrderedMembers()
    {
        await using var context = _dbFixture.CreateContext();
        var auditMock = new Mock<IAuditService>();
        var service = new MemberService(context, auditMock.Object);

        var fee = CreateAdultFee();
        context.MembershipFees.Add(fee);

        var alice = CreateAdultMember(0, "alice@example.com", fee);
        alice.FirstName = "Alice";
        alice.LastName = "Alpha";
        var bob = CreateAdultMember(0, "bob@example.com", fee);
        bob.FirstName = "Bob";
        bob.LastName = "Beta";

        context.Members.AddRange(alice, bob);
        await context.SaveChangesAsync();

        var all = await service.GetAllAsync();

        all.Should().HaveCount(2);
        all[0].LastName.Should().Be("Alpha");
        all[1].LastName.Should().Be("Beta");
    }
}