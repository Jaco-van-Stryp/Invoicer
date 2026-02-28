using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Estimate.CreateEstimate;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Estimate.CreateEstimate;

[Collection("Database")]
public class CreateEstimateHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Client Client,
        Product ProductA,
        Product ProductB
    )> SeedFullScenarioAsync()
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
        var productA = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget A",
            Price = 10m,
            Description = "Widget A desc",
            CompanyId = company.Id,
            Company = company,
        };
        var productB = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget B",
            Price = 25m,
            Description = "Widget B desc",
            CompanyId = company.Id,
            Company = company,
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddRangeAsync(productA, productB);
        await DbContext.SaveChangesAsync();
        return (user, company, client, productA, productB);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesEstimateWithProducts()
    {
        // Arrange
        var (user, company, client, productA, productB) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var estimateDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var expiresOn = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        var command = new CreateEstimateCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            EstimateDate: estimateDate,
            ExpiresOn: expiresOn,
            Status: EstimateStatus.Draft,
            Notes: "Test notes",
            Products:
            [
                new CreateEstimateProductItem(productA.Id, 2),
                new CreateEstimateProductItem(productB.Id, 3),
            ]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — response
        result.EstimateId.Should().NotBeEmpty();
        result.EstimateNumber.Should().Be("EST-0001");

        // Assert — persistence
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Estimates.Include(e => e.ProductEstimates)
            .FirstOrDefaultAsync(e => e.Id == result.EstimateId);
        saved.Should().NotBeNull();
        saved!.EstimateNumber.Should().Be("EST-0001");
        saved.EstimateDate.Should().Be(estimateDate);
        saved.ExpiresOn.Should().Be(expiresOn);
        saved.Status.Should().Be(EstimateStatus.Draft);
        saved.Notes.Should().Be("Test notes");
        saved.ClientId.Should().Be(client.Id);
        saved.CompanyId.Should().Be(company.Id);
        saved.ProductEstimates.Should().HaveCount(2);
        saved.TotalAmount.Should().Be(2 * 10m + 3 * 25m); // 20 + 75 = 95

        var peA = saved.ProductEstimates.First(pe => pe.ProductId == productA.Id);
        peA.Quantity.Should().Be(2);
        peA.UnitPrice.Should().Be(10m);

        var peB = saved.ProductEstimates.First(pe => pe.ProductId == productB.Id);
        peB.Quantity.Should().Be(3);
        peB.UnitPrice.Should().Be(25m);
    }

    [Fact]
    public async Task Handle_SingleProduct_CreatesEstimateSuccessfully()
    {
        // Arrange
        var (user, company, client, productA, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Sent,
            Notes: null,
            Products: [new CreateEstimateProductItem(productA.Id, 1)]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.EstimateNumber.Should().Be("EST-0001");
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Estimates.Include(e => e.ProductEstimates)
            .FirstOrDefaultAsync(e => e.Id == result.EstimateId);
        saved.Should().NotBeNull();
        saved!.ProductEstimates.Should().HaveCount(1);
        saved.Notes.Should().BeNull();
        saved.TotalAmount.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_MultipleEstimates_IncrementsEstimateNumber()
    {
        // Arrange
        var (user, company, client, productA, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Draft,
            Notes: null,
            Products: [new CreateEstimateProductItem(productA.Id, 1)]
        );

        // Act
        var result1 = await handler.Handle(command, CancellationToken.None);
        var result2 = await handler.Handle(command, CancellationToken.None);

        // Assert
        result1.EstimateNumber.Should().Be("EST-0001");
        result2.EstimateNumber.Should().Be("EST-0002");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Draft,
            Notes: null,
            Products: [new CreateEstimateProductItem(Guid.NewGuid(), 1)]
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
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Draft,
            Notes: null,
            Products: [new CreateEstimateProductItem(Guid.NewGuid(), 1)]
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentClient_ThrowsClientNotFoundException()
    {
        // Arrange
        var (user, company, _, productA, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: company.Id,
            ClientId: Guid.NewGuid(), // non-existent client
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Draft,
            Notes: null,
            Products: [new CreateEstimateProductItem(productA.Id, 1)]
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var (user, company, client, _, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Draft,
            Notes: null,
            Products: [new CreateEstimateProductItem(Guid.NewGuid(), 1)] // non-existent product
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (_, company, client, productA, _) = await SeedFullScenarioAsync();
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
        var handler = new CreateEstimateHandler(DbContext, CurrentUserService);

        var command = new CreateEstimateCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            EstimateDate: DateTime.UtcNow,
            ExpiresOn: DateTime.UtcNow.AddDays(30),
            Status: EstimateStatus.Draft,
            Notes: null,
            Products: [new CreateEstimateProductItem(productA.Id, 1)]
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
