using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Client.DeleteClient;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Client.DeleteClient;

[Collection("Database")]
public class DeleteClientHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Client Client
    )> SeedUserWithCompanyAndClientAsync()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@test.com",
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };
        var company = new Domain.Entities.Company
        {
            Id = Guid.NewGuid(),
            Name = "Test Corp",
            Address = "123 Test St",
            TaxNumber = "TAX-123",
            PhoneNumber = "555-0100",
            Email = "test@corp.com",
            PaymentDetails = "Bank: Test",
            LogoUrl = "https://test.com/logo.png",
            UserId = user.Id,
            User = user,
        };
        var client = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            Name = "Doomed Client",
            Email = "doomed@test.com",
            Address = "123 Doomed St",
            TaxNumber = "TAX-DOOM",
            PhoneNumber = "555-0666",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.SaveChangesAsync();
        return (user, company, client);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesClientFromDatabase()
    {
        // Arrange
        var (user, company, client) = await SeedUserWithCompanyAndClientAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteClientHandler(DbContext, CurrentUserService);

        var command = new DeleteClientCommand(CompanyId: company.Id, ClientId: client.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Clients.FindAsync(client.Id);
        deleted.Should().BeNull("the client should have been removed from the database");
    }

    [Fact]
    public async Task Handle_DeleteOneClient_OtherClientsRemain()
    {
        // Arrange
        var (user, company, client1) = await SeedUserWithCompanyAndClientAsync();
        var client2 = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            Name = "Survivor Client",
            Email = "survivor@test.com",
            Address = "456 Safe St",
            TaxNumber = "TAX-SAFE",
            PhoneNumber = "555-0777",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Clients.AddAsync(client2);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteClientHandler(DbContext, CurrentUserService);

        // Act — delete only the first client
        await handler.Handle(
            new DeleteClientCommand(company.Id, client1.Id),
            CancellationToken.None
        );

        // Assert
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Clients.FindAsync(client1.Id);
        deleted.Should().BeNull();

        var remaining = await DbContext.Clients.FindAsync(client2.Id);
        remaining.Should().NotBeNull();
        remaining!.Name.Should().Be("Survivor Client");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new DeleteClientHandler(DbContext, CurrentUserService);

        var command = new DeleteClientCommand(CompanyId: Guid.NewGuid(), ClientId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentCompany_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@test.com",
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteClientHandler(DbContext, CurrentUserService);

        var command = new DeleteClientCommand(CompanyId: Guid.NewGuid(), ClientId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentClient_ThrowsClientNotFoundException()
    {
        // Arrange
        var (user, company, _) = await SeedUserWithCompanyAndClientAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteClientHandler(DbContext, CurrentUserService);

        var command = new DeleteClientCommand(CompanyId: company.Id, ClientId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (user1, company, client) = await SeedUserWithCompanyAndClientAsync();
        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@test.com",
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };
        await DbContext.Users.AddAsync(user2);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user2.Id, user2.Email);
        var handler = new DeleteClientHandler(DbContext, CurrentUserService);

        var command = new DeleteClientCommand(CompanyId: company.Id, ClientId: client.Id);

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
