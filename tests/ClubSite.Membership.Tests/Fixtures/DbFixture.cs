namespace ClubSite.Membership.Tests.Fixtures;

public class DbFixture : IDisposable
{
    public MembershipDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MembershipDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new MembershipDbContext(options);
    }

    public void Dispose() { }
}