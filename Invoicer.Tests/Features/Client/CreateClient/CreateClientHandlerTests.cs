using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Client.CreateClient;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Client.CreateClient;

[Collection("Database")]
public class CreateClientHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(User User, Domain.Entities.Company Company)> SeedUserWithCompanyAsync()
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
        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.SaveChangesAsync();
        return (user, company);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesClientInDatabase()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        var command = new CreateClientCommand(
            Name: "Acme Inc",
            Email: "acme@test.com",
            Address: "456 Client St",
            TaxNumber: "TAX-CLIENT-1",
            PhoneNumber: "555-0200",
            CompanyId: company.Id
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — verify response
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Acme Inc");
        result.Email.Should().Be("acme@test.com");
        result.Address.Should().Be("456 Client St");
        result.TaxNumber.Should().Be("TAX-CLIENT-1");
        result.PhoneNumber.Should().Be("555-0200");
        result.CompanyId.Should().Be(company.Id);

        // Assert — verify persistence
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Clients.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Acme Inc");
        saved.Email.Should().Be("acme@test.com");
        saved.Address.Should().Be("456 Client St");
        saved.TaxNumber.Should().Be("TAX-CLIENT-1");
        saved.PhoneNumber.Should().Be("555-0200");
        saved.CompanyId.Should().Be(company.Id);
    }

    [Fact]
    public async Task Handle_NullOptionalFields_CreatesClientWithNulls()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        var command = new CreateClientCommand(
            Name: "Minimal Client",
            Email: "minimal@test.com",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            CompanyId: company.Id
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Address.Should().BeNull();
        result.TaxNumber.Should().BeNull();
        result.PhoneNumber.Should().BeNull();

        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Clients.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Address.Should().BeNull();
        saved.TaxNumber.Should().BeNull();
        saved.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleClients_AllPersisted()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        // Act
        var result1 = await handler.Handle(
            new CreateClientCommand("Client A", "a@test.com", null, null, null, company.Id),
            CancellationToken.None
        );
        var result2 = await handler.Handle(
            new CreateClientCommand("Client B", "b@test.com", null, null, null, company.Id),
            CancellationToken.None
        );

        // Assert
        result1.Id.Should().NotBe(result2.Id);

        DbContext.ChangeTracker.Clear();
        var clients = await DbContext.Clients.Where(c => c.CompanyId == company.Id).ToListAsync();
        clients.Should().HaveCount(2);
        clients.Select(c => c.Name).Should().Contain(["Client A", "Client B"]);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsClientAlreadyExistsException()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        await handler.Handle(
            new CreateClientCommand("First Client", "dupe@test.com", null, null, null, company.Id),
            CancellationToken.None
        );

        var duplicateCommand = new CreateClientCommand(
            Name: "Second Client",
            Email: "dupe@test.com",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            CompanyId: company.Id
        );

        // Act & Assert
        var act = () => handler.Handle(duplicateCommand, CancellationToken.None);
        await act.Should().ThrowAsync<ClientAlreadyExistsException>();
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        var command = new CreateClientCommand(
            Name: "Ghost Client",
            Email: "ghost@test.com",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            CompanyId: Guid.NewGuid()
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
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        var command = new CreateClientCommand(
            Name: "Orphan Client",
            Email: "orphan@test.com",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            CompanyId: Guid.NewGuid()
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange — company belongs to user1
        var (user1, company) = await SeedUserWithCompanyAsync();
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

        // Authenticate as user2
        SetCurrentUser(user2.Id, user2.Email);
        var handler = new CreateClientHandler(DbContext, CurrentUserService);

        var command = new CreateClientCommand(
            Name: "Hijacked Client",
            Email: "hijack@test.com",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            CompanyId: company.Id
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
