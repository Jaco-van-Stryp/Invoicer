using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Client.GetAllClients;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Client.GetAllClients;

[Collection("Database")]
public class GetAllClientsHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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

    private async Task<Domain.Entities.Client> SeedClientAsync(
        Domain.Entities.Company company,
        string name,
        string email
    )
    {
        var client = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Address = $"Address for {name}",
            TaxNumber = $"TAX-{name}",
            PhoneNumber = "555-0300",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Clients.AddAsync(client);
        await DbContext.SaveChangesAsync();
        return client;
    }

    [Fact]
    public async Task Handle_CompanyWithClients_ReturnsAllClients()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        await SeedClientAsync(company, "Client A", "a@test.com");
        await SeedClientAsync(company, "Client B", "b@test.com");
        await SeedClientAsync(company, "Client C", "c@test.com");

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllClientsHandler(DbContext, CurrentUserService);

        var query = new GetAllClientsQuery(CompanyId: company.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(c => c.Name).Should().Contain(["Client A", "Client B", "Client C"]);
        result.Select(c => c.Email).Should().Contain(["a@test.com", "b@test.com", "c@test.com"]);
    }

    [Fact]
    public async Task Handle_CompanyWithNoClients_ReturnsEmptyList()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllClientsHandler(DbContext, CurrentUserService);

        var query = new GetAllClientsQuery(CompanyId: company.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAllClientFields()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        var client = await SeedClientAsync(company, "Full Client", "full@test.com");

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllClientsHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(
            new GetAllClientsQuery(company.Id),
            CancellationToken.None
        );

        // Assert
        result.Should().HaveCount(1);
        var returned = result[0];
        returned.Id.Should().Be(client.Id);
        returned.Name.Should().Be("Full Client");
        returned.Email.Should().Be("full@test.com");
        returned.Address.Should().Be("Address for Full Client");
        returned.TaxNumber.Should().Be("TAX-Full Client");
        returned.PhoneNumber.Should().Be("555-0300");
    }

    [Fact]
    public async Task Handle_ClientsFromOtherCompany_NotReturned()
    {
        // Arrange — two companies for the same user
        var (user, company1) = await SeedUserWithCompanyAsync();
        var company2 = new Domain.Entities.Company
        {
            Id = Guid.NewGuid(),
            Name = "Other Corp",
            Address = "456 Other St",
            TaxNumber = "TAX-456",
            PhoneNumber = "555-0200",
            Email = "other@corp.com",
            PaymentDetails = "Bank: Other",
            LogoUrl = "https://other.com/logo.png",
            UserId = user.Id,
            User = user,
        };
        await DbContext.Companies.AddAsync(company2);
        await DbContext.SaveChangesAsync();

        await SeedClientAsync(company1, "Company1 Client", "c1@test.com");
        await SeedClientAsync(company2, "Company2 Client", "c2@test.com");

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllClientsHandler(DbContext, CurrentUserService);

        // Act — query only company1's clients
        var result = await handler.Handle(
            new GetAllClientsQuery(CompanyId: company1.Id),
            CancellationToken.None
        );

        // Assert — should only return company1's client
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Company1 Client");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new GetAllClientsHandler(DbContext, CurrentUserService);

        var query = new GetAllClientsQuery(CompanyId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(query, CancellationToken.None);
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
        var handler = new GetAllClientsHandler(DbContext, CurrentUserService);

        var query = new GetAllClientsQuery(CompanyId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
