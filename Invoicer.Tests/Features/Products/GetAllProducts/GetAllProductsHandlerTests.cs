using FluentAssertions;
using global::Invoicer.Features.Products.GetAllProducts;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Products.GetAllProducts;

[Collection("Database")]
public class GetAllProductsHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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

    private async Task SeedProductAsync(Domain.Entities.Company company, string name, decimal price)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Description = $"Description for {name}",
            ImageUrl = $"https://test.com/{name.ToLower()}.png",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Products.AddAsync(product);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_CompanyWithProducts_ReturnsAllProducts()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        await SeedProductAsync(company, "Widget", 10m);
        await SeedProductAsync(company, "Gadget", 25m);
        await SeedProductAsync(company, "Doohickey", 5.50m);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllProductsHandler(DbContext, CurrentUserService);

        var query = new GetAllProductsQuery(CompanyId: company.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(p => p.Name).Should().Contain(["Widget", "Gadget", "Doohickey"]);
        result.Select(p => p.Price).Should().Contain([10m, 25m, 5.50m]);
    }

    [Fact]
    public async Task Handle_CompanyWithNoProducts_ReturnsEmptyList()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllProductsHandler(DbContext, CurrentUserService);

        var query = new GetAllProductsQuery(CompanyId: company.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new GetAllProductsHandler(DbContext, CurrentUserService);

        var query = new GetAllProductsQuery(CompanyId: Guid.NewGuid());

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
        var handler = new GetAllProductsHandler(DbContext, CurrentUserService);

        var query = new GetAllProductsQuery(CompanyId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_ProductsFromOtherCompany_NotReturned()
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

        await SeedProductAsync(company1, "Company1 Product", 10m);
        await SeedProductAsync(company2, "Company2 Product", 20m);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllProductsHandler(DbContext, CurrentUserService);

        // Act — query only company1's products
        var result = await handler.Handle(
            new GetAllProductsQuery(CompanyId: company1.Id),
            CancellationToken.None
        );

        // Assert — should only return company1's product
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Company1 Product");
    }
}
