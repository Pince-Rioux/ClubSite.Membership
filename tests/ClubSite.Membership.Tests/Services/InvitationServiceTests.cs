using ClubSite.Membership.Data;
using ClubSite.Membership.Models;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Services.Tests;

public sealed class InvitationServiceTests : IDisposable
{
    private readonly MembershipDbContext _context;
    private readonly InvitationService _service;

    public InvitationServiceTests()
    {
        var options = new DbContextOptionsBuilder<MembershipDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new MembershipDbContext(options);
        _service = new InvitationService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private async Task<Member> SeedMemberAsync(string email = "test@example.com")
    {
        var fee = new MembershipFee
        {
            Name = "Adult Fee",
            Amount = 50m,
            Interval = FeeInterval.Yearly,
            Category = FeeCategory.Adult,
            IsDefault = true
        };
        _context.MembershipFees.Add(fee);
        await _context.SaveChangesAsync();

        var member = new Member
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            BirthDate = new DateTime(1990, 1, 1),
            MembershipFeeId = fee.Id,
            Status = MemberStatus.Active,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        _context.Members.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    // ----------------------------------------------------------------------- //
    //  CreateInvitationAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task CreateInvitation_GeneratesValidToken()
    {
        var member = await SeedMemberAsync();

        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        invitation.Should().NotBeNull();
        invitation.Id.Should().NotBeEmpty();
        invitation.MemberId.Should().Be(member.Id);
        invitation.Email.Should().Be(member.Email);
        invitation.Token.Should().NotBeNullOrWhiteSpace();
        invitation.Token.Length.Should().BeGreaterThan(20); // 32 bytes → ~43 chars base64
        invitation.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        invitation.CompletedAt.Should().BeNull();
        invitation.Attempts.Should().Be(0);
    }

    [Fact]
    public async Task CreateInvitation_CancelsPreviousPendingInvitations()
    {
        var member = await SeedMemberAsync();

        // Create first invitation
        var first = await _service.CreateInvitationAsync(member.Id, member.Email);
        Thread.Sleep(100); // ensure different CreatedAt

        // Create second invitation
        var second = await _service.CreateInvitationAsync(member.Id, member.Email);

        // First should now be expired
        var expiredFirst = await _context.Invitations.FindAsync(first.Id);
        expiredFirst.Should().NotBeNull();
        expiredFirst!.ExpiresAt.Should().BeBefore(DateTime.UtcNow);

        // Second should be valid
        second.CompletedAt.Should().BeNull();
        second.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    // ----------------------------------------------------------------------- //
    //  ValidateTokenAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsInvitation()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        var result = await _service.ValidateTokenAsync(invitation.Token);

        result.Should().NotBeNull();
        result!.Id.Should().Be(invitation.Id);
        result.Member.Should().NotBeNull();
        result.Member!.Email.Should().Be(member.Email);
    }

    [Fact]
    public async Task ValidateToken_NullToken_ReturnsNull()
    {
        var result = await _service.ValidateTokenAsync(null!);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_EmptyToken_ReturnsNull()
    {
        var result = await _service.ValidateTokenAsync("");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_NonExistentToken_ReturnsNull()
    {
        var result = await _service.ValidateTokenAsync("nonexistent-token-12345");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ExpiredToken_ReturnsNull()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        // Manually expire it
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        var result = await _service.ValidateTokenAsync(invitation.Token);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_CompletedToken_ReturnsNull()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        await _service.CompleteInvitationAsync(invitation.Id);

        var result = await _service.ValidateTokenAsync(invitation.Token);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ExceedsMaxAttempts_ReturnsNull()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        // Simulate 5 failed attempts
        invitation.Attempts = 5;
        await _context.SaveChangesAsync();

        var result = await _service.ValidateTokenAsync(invitation.Token);
        result.Should().BeNull();
    }

    // ----------------------------------------------------------------------- //
    //  CompleteInvitationAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task CompleteInvitation_SetsCompletedAt()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        await _service.CompleteInvitationAsync(invitation.Id);

        var fromDb = await _context.Invitations.FindAsync(invitation.Id);
        fromDb.Should().NotBeNull();
        fromDb!.CompletedAt.Should().NotBeNull();
        fromDb.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CompleteInvitation_NonExistent_DoesNotThrow()
    {
        var act = () => _service.CompleteInvitationAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    // ----------------------------------------------------------------------- //
    //  RecordFailedAttemptAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task RecordFailedAttempt_IncrementsCounter()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        await _service.RecordFailedAttemptAsync(invitation.Id);

        var fromDb = await _context.Invitations.FindAsync(invitation.Id);
        fromDb!.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task MultipleFailedAttempts_LocksTokenAfter5()
    {
        var member = await SeedMemberAsync();
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        for (int i = 0; i < 5; i++)
        {
            await _service.RecordFailedAttemptAsync(invitation.Id);
        }

        // Validate should now return null
        var result = await _service.ValidateTokenAsync(invitation.Token);
        result.Should().BeNull();
    }

    // ----------------------------------------------------------------------- //
    //  GetPendingInvitationsAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task GetPendingInvitations_ReturnsActiveOnly()
    {
        // Create invitations for different members so they don't cancel each other
        var member1 = await SeedMemberAsync("m1@example.com");
        var member2 = await SeedMemberAsync("m2@example.com");
        var member3 = await SeedMemberAsync("m3@example.com");

        var active = await _service.CreateInvitationAsync(member1.Id, member1.Email);

        // Expire member2's invitation manually
        var expired = await _service.CreateInvitationAsync(member2.Id, member2.Email);
        expired.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _context.SaveChangesAsync();

        // Complete member3's invitation
        var completed = await _service.CreateInvitationAsync(member3.Id, member3.Email);
        await _service.CompleteInvitationAsync(completed.Id);

        var pending = await _service.GetPendingInvitationsAsync(member1.Email);

        pending.Should().HaveCount(1);
        pending[0].Id.Should().Be(active.Id);
    }

    // ----------------------------------------------------------------------- //
    //  CancelPendingInvitationsAsync tests
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task CancelPendingInvitations_ExpiresAllActive()
    {
        var member = await SeedMemberAsync();
        var inv1 = await _service.CreateInvitationAsync(member.Id, member.Email);
        var inv2 = await _service.CreateInvitationAsync(member.Id, member.Email);

        await _service.CancelPendingInvitationsAsync(member.Id);

        var fromDb1 = await _context.Invitations.FindAsync(inv1.Id);
        var fromDb2 = await _context.Invitations.FindAsync(inv2.Id);

        fromDb1!.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
        fromDb2!.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
    }

    // ----------------------------------------------------------------------- //
    //  Integration: full activation flow
    // ----------------------------------------------------------------------- //

    [Fact]
    public async Task FullActivationFlow_FromCreateToComplete_Succeeds()
    {
        var member = await SeedMemberAsync("activate-flow@example.com");

        // 1. Create invitation
        var invitation = await _service.CreateInvitationAsync(member.Id, member.Email);

        // 2. Validate token
        var validated = await _service.ValidateTokenAsync(invitation.Token);
        validated.Should().NotBeNull();
        validated!.MemberId.Should().Be(member.Id);

        // 3. Complete invitation
        await _service.CompleteInvitationAsync(invitation.Id);

        // 4. Token is now consumed
        var afterComplete = await _service.ValidateTokenAsync(invitation.Token);
        afterComplete.Should().BeNull();
    }
}