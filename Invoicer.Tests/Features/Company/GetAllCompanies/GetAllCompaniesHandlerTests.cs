using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Features.Company.GetAllCompanies;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Company.GetAllCompanies;

[Collection("Database")]
public class GetAllCompaniesHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<User> SeedUserWithCompaniesAsync(string email, params string[] companyNames)
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
        await DbContext.Users.AddAsync(user);

        foreach (var name in companyNames)
        {
            await DbContext.Companies.AddAsync(new Domain.Entities.Company
            {
                Id = Guid.NewGuid(),
                Name = name,
                Address = "123 Test St",
                TaxNumber = "TAX-000",
                PhoneNumber = "555-0000",
                Email = $"{name.ToLower().Replace(" ", "")}@test.com",
                PaymentDetails = "Bank: Test",
                LogoUrl = "",
                UserId = user.Id,
                User = user,
            });
        }

        await DbContext.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Handle_UserWithNoCompanies_ReturnsEmptyList()
    {
        // Arrange
        var user = await SeedUserWithCompaniesAsync("empty@test.com");
        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllCompaniesHandler(DbContext, CurrentUserService);

        var query = new GetAllCompaniesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithCompanies_ReturnsAllCompanies()
    {
        // Arrange
        var user = await SeedUserWithCompaniesAsync("owner@test.com", "Company A", "Company B");
        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllCompaniesHandler(DbContext, CurrentUserService);

        var query = new GetAllCompaniesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Name).Should().Contain(["Company A", "Company B"]);
    }

    [Fact]
    public async Task Handle_DoesNotReturnOtherUsersCompanies()
    {
        // Arrange
        var user1 = await SeedUserWithCompaniesAsync("user1@test.com", "User1 Corp");
        var user2 = await SeedUserWithCompaniesAsync("user2@test.com", "User2 Corp");

        SetCurrentUser(user1.Id, user1.Email);
        var handler = new GetAllCompaniesHandler(DbContext, CurrentUserService);

        var query = new GetAllCompaniesQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("User1 Corp");
    }

    [Fact]
    public async Task Handle_ReturnsAllCompanyFields()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fields@test.com",
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };
        await DbContext.Users.AddAsync(user);

        var company = new Domain.Entities.Company
        {
            Id = Guid.NewGuid(),
            Name = "Full Details Corp",
            Address = "789 Full St",
            TaxNumber = "TAX-FULL",
            PhoneNumber = "555-FULL",
            Email = "full@test.com",
            PaymentDetails = "Bank: Full, Acc: 999",
            LogoUrl = "https://full.com/logo.png",
            UserId = user.Id,
            User = user,
        };
        await DbContext.Companies.AddAsync(company);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllCompaniesHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllCompaniesQuery(), CancellationToken.None);

        // Assert
        var returned = result.Should().ContainSingle().Subject;
        returned.Id.Should().Be(company.Id);
        returned.Name.Should().Be("Full Details Corp");
        returned.Address.Should().Be("789 Full St");
        returned.TaxNumber.Should().Be("TAX-FULL");
        returned.PhoneNumber.Should().Be("555-FULL");
        returned.Email.Should().Be("full@test.com");
        returned.PaymentDetails.Should().Be("Bank: Full, Acc: 999");
        returned.LogoUrl.Should().Be("https://full.com/logo.png");
    }
}
