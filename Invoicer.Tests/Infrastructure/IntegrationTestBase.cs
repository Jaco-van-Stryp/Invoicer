using Invoicer.Domain.Data;
using Invoicer.Infrastructure.CurrentUserService;
using NSubstitute;

namespace Invoicer.Tests.Infrastructure;

[Collection("Database")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly DatabaseFixture _db;

    protected AppDbContext DbContext { get; private set; } = null!;

    protected ICurrentUserService CurrentUserService { get; } =
        Substitute.For<ICurrentUserService>();

    protected IntegrationTestBase(DatabaseFixture db)
    {
        _db = db;
    }

    /// <summary>
    /// Configures the mocked ICurrentUserService to return the given user identity.
    /// Call this in your test's Arrange phase.
    /// </summary>
    protected void SetCurrentUser(Guid userId, string email = "test@test.com")
    {
        CurrentUserService.UserId.Returns(userId);
        CurrentUserService.Email.Returns(email);
    }

    public async Task InitializeAsync()
    {
        // Reset DB to clean state before each test
        await _db.ResetDatabaseAsync();
        DbContext = _db.CreateDbContext();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
    }
}
