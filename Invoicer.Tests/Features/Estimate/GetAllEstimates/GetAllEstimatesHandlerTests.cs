using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Estimate.GetAllEstimates;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Estimate.GetAllEstimates;

[Collection("Database")]
public class GetAllEstimatesHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Client Client,
        Product Product
    )> SeedUserWithCompanyClientAndProductAsync()
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
            Name = "Test Client",
            Email = "client@test.com",
            Address = "456 Client St",
            CompanyId = company.Id,
            Company = company,
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = 15m,
            Description = "A widget",
            CompanyId = company.Id,
            Company = company,
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.SaveChangesAsync();
        return (user, company, client, product);
    }

    private async Task<Domain.Entities.Estimate> SeedEstimateAsync(
        Domain.Entities.Company company,
        Domain.Entities.Client client,
        Product product,
        string estimateNumber,
        int quantity,
        DateTime? estimateDate = null
    )
    {
        var date = estimateDate ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var estimate = new Domain.Entities.Estimate
        {
            Id = Guid.NewGuid(),
            EstimateNumber = estimateNumber,
            EstimateDate = date,
            ExpiresOn = date.AddDays(30),
            Status = EstimateStatus.Draft,
            ClientId = client.Id,
            Client = client,
            CompanyId = company.Id,
            Company = company,
            ProductEstimates = [],
        };
        var productEstimate = new ProductEstimate
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            EstimateId = estimate.Id,
            Estimate = estimate,
            CompanyId = company.Id,
            Company = company,
            Quantity = quantity,
            UnitPrice = product.Price,
        };
        estimate.ProductEstimates.Add(productEstimate);
        await DbContext.Estimates.AddAsync(estimate);
        await DbContext.SaveChangesAsync();
        return estimate;
    }

    [Fact]
    public async Task Handle_CompanyWithEstimates_ReturnsAllEstimates()
    {
        // Arrange
        var (user, company, client, product) = await SeedUserWithCompanyClientAndProductAsync();
        await SeedEstimateAsync(company, client, product, "EST-001", 2);
        await SeedEstimateAsync(company, client, product, "EST-002", 5);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllEstimatesHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllEstimatesQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(e => e.EstimateNumber).Should().Contain(["EST-001", "EST-002"]);
    }

    [Fact]
    public async Task Handle_CompanyWithNoEstimates_ReturnsEmptyList()
    {
        // Arrange
        var (user, company, _, _) = await SeedUserWithCompanyClientAndProductAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllEstimatesHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllEstimatesQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAllEstimateFieldsIncludingProducts()
    {
        // Arrange
        var (user, company, client, product) = await SeedUserWithCompanyClientAndProductAsync();
        var estimateDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var estimate = await SeedEstimateAsync(company, client, product, "EST-FULL", 3, estimateDate);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllEstimatesHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllEstimatesQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var returned = result[0];
        returned.Id.Should().Be(estimate.Id);
        returned.EstimateNumber.Should().Be("EST-FULL");
        returned.EstimateDate.Should().Be(estimateDate);
        returned.ExpiresOn.Should().Be(estimateDate.AddDays(30));
        returned.Status.Should().Be(EstimateStatus.Draft);
        returned.TotalAmount.Should().Be(3 * 15m);
        returned.ClientId.Should().Be(client.Id);
        returned.ClientName.Should().Be("Test Client");

        returned.Products.Should().HaveCount(1);
        var returnedProduct = returned.Products[0];
        returnedProduct.ProductId.Should().Be(product.Id);
        returnedProduct.ProductName.Should().Be("Widget");
        returnedProduct.UnitPrice.Should().Be(15m);
        returnedProduct.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Handle_EstimatesFromOtherCompany_NotReturned()
    {
        // Arrange
        var (user, company1, client1, product1) = await SeedUserWithCompanyClientAndProductAsync();

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
        var client2 = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            Name = "Other Client",
            Email = "other-client@test.com",
            CompanyId = company2.Id,
            Company = company2,
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Other Widget",
            Price = 20m,
            Description = "Other widget",
            CompanyId = company2.Id,
            Company = company2,
        };
        await DbContext.Companies.AddAsync(company2);
        await DbContext.Clients.AddAsync(client2);
        await DbContext.Products.AddAsync(product2);
        await DbContext.SaveChangesAsync();

        await SeedEstimateAsync(company1, client1, product1, "EST-C1", 1);
        await SeedEstimateAsync(company2, client2, product2, "EST-C2", 1);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllEstimatesHandler(DbContext, CurrentUserService);

        // Act — query only company1
        var result = await handler.Handle(new GetAllEstimatesQuery(company1.Id), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].EstimateNumber.Should().Be("EST-C1");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new GetAllEstimatesHandler(DbContext, CurrentUserService);

        // Act & Assert
        var act = () => handler.Handle(new GetAllEstimatesQuery(Guid.NewGuid()), CancellationToken.None);
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
        var handler = new GetAllEstimatesHandler(DbContext, CurrentUserService);

        // Act & Assert
        var act = () => handler.Handle(new GetAllEstimatesQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
