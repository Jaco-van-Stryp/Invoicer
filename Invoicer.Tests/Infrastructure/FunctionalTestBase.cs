using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicer.Tests.Infrastructure;

[Collection("Database")]
public abstract class FunctionalTestBase : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private InvoicerApiFactory _factory = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected FunctionalTestBase(DatabaseFixture db)
    {
        _db = db;
    }

    protected HttpClient CreateAuthenticatedClient(Guid userId, string email = "test@test.com")
    {
        return _factory.CreateAuthenticatedClient(userId, email);
    }

    protected async Task<(HttpClient Client, User User)> CreateAuthenticatedUserAsync(
        string email = "test@test.com"
    )
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };

        using var dbContext = _db.CreateDbContext();
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();

        var client = CreateAuthenticatedClient(user.Id, email);
        return (client, user);
    }

    protected AppDbContext CreateDbContext() => _db.CreateDbContext();

    public async Task InitializeAsync()
    {
        await _db.ResetDatabaseAsync();
        _factory = new InvoicerApiFactory(_db);
        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
