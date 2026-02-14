using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Client.UpdateClient;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Client.UpdateClient;

[Collection("Database")]
public class UpdateClientHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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
            Name = "Original Client",
            Email = "original@test.com",
            Address = "Original Address",
            TaxNumber = "TAX-ORIG",
            PhoneNumber = "555-0300",
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
    public async Task Handle_AllFieldsProvided_UpdatesAllFields()
    {
        // Arrange
        var (user, company, client) = await SeedUserWithCompanyAndClientAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            Name: "Updated Client",
            Email: "updated@test.com",
            Address: "Updated Address",
            TaxNumber: "TAX-UPD",
            PhoneNumber: "555-0999"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Clients.FindAsync(client.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Updated Client");
        saved.Email.Should().Be("updated@test.com");
        saved.Address.Should().Be("Updated Address");
        saved.TaxNumber.Should().Be("TAX-UPD");
        saved.PhoneNumber.Should().Be("555-0999");
    }

    [Fact]
    public async Task Handle_OnlyNameProvided_UpdatesOnlyName()
    {
        // Arrange
        var (user, company, client) = await SeedUserWithCompanyAndClientAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            Name: "New Name Only",
            Email: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Clients.FindAsync(client.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Name Only");
        saved.Email.Should().Be("original@test.com");
        saved.Address.Should().Be("Original Address");
        saved.TaxNumber.Should().Be("TAX-ORIG");
        saved.PhoneNumber.Should().Be("555-0300");
    }

    [Fact]
    public async Task Handle_OnlyEmailProvided_UpdatesOnlyEmail()
    {
        // Arrange
        var (user, company, client) = await SeedUserWithCompanyAndClientAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            Name: null,
            Email: "newemail@test.com",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Clients.FindAsync(client.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Original Client");
        saved.Email.Should().Be("newemail@test.com");
        saved.Address.Should().Be("Original Address");
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_NothingChanges()
    {
        // Arrange
        var (user, company, client) = await SeedUserWithCompanyAndClientAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            Name: null,
            Email: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Clients.FindAsync(client.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Original Client");
        saved.Email.Should().Be("original@test.com");
        saved.Address.Should().Be("Original Address");
        saved.TaxNumber.Should().Be("TAX-ORIG");
        saved.PhoneNumber.Should().Be("555-0300");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            Name: "Doesn't Matter",
            Email: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

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
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            Name: "No Company",
            Email: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

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
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: company.Id,
            ClientId: Guid.NewGuid(),
            Name: "No Client",
            Email: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

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
        var handler = new UpdateClientHandler(DbContext, CurrentUserService);

        var command = new UpdateClientCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            Name: "Hijacked",
            Email: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
