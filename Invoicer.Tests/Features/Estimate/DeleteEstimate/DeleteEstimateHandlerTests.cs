using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Estimate.DeleteEstimate;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Estimate.DeleteEstimate;

[Collection("Database")]
public class DeleteEstimateHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Estimate Estimate
    )> SeedUserWithCompanyAndEstimateAsync()
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
            Price = 10m,
            Description = "A widget",
            CompanyId = company.Id,
            Company = company,
        };
        var estimate = new Domain.Entities.Estimate
        {
            Id = Guid.NewGuid(),
            EstimateNumber = "EST-DOOMED",
            EstimateDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresOn = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
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
            Quantity = 2,
            UnitPrice = product.Price,
        };
        estimate.ProductEstimates.Add(productEstimate);

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.Estimates.AddAsync(estimate);
        await DbContext.SaveChangesAsync();
        return (user, company, estimate);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesEstimateAndProductEstimates()
    {
        // Arrange
        var (user, _, estimate) = await SeedUserWithCompanyAndEstimateAsync();
        var estimateId = estimate.Id;
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteEstimateHandler(DbContext, CurrentUserService);

        // Act
        await handler.Handle(new DeleteEstimateCommand(EstimateId: estimateId), CancellationToken.None);

        // Assert — estimate is deleted
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Estimates.FindAsync(estimateId);
        deleted.Should().BeNull("the estimate should have been removed from the database");

        // Assert — product estimates are also deleted (cascade)
        var productEstimates = await DbContext
            .ProductEstimates.Where(pe => pe.EstimateId == estimateId)
            .ToListAsync();
        productEstimates.Should().BeEmpty("product-estimate join records should also be removed");
    }

    [Fact]
    public async Task Handle_DeleteOneEstimate_OtherEstimatesRemain()
    {
        // Arrange
        var (user, company, estimate1) = await SeedUserWithCompanyAndEstimateAsync();

        var client = await DbContext.Clients.FirstAsync(c => c.CompanyId == company.Id);

        var estimate2 = new Domain.Entities.Estimate
        {
            Id = Guid.NewGuid(),
            EstimateNumber = "EST-SURVIVOR",
            EstimateDate = DateTime.UtcNow,
            ExpiresOn = DateTime.UtcNow.AddDays(30),
            Status = EstimateStatus.Draft,
            ClientId = client.Id,
            Client = client,
            CompanyId = company.Id,
            Company = company,
            ProductEstimates = [],
        };
        await DbContext.Estimates.AddAsync(estimate2);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteEstimateHandler(DbContext, CurrentUserService);

        // Act — delete only the first estimate
        await handler.Handle(
            new DeleteEstimateCommand(EstimateId: estimate1.Id),
            CancellationToken.None
        );

        // Assert
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Estimates.FindAsync(estimate1.Id);
        deleted.Should().BeNull();

        var remaining = await DbContext.Estimates.FindAsync(estimate2.Id);
        remaining.Should().NotBeNull();
        remaining!.EstimateNumber.Should().Be("EST-SURVIVOR");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new DeleteEstimateHandler(DbContext, CurrentUserService);

        // Act & Assert
        var act = () =>
            handler.Handle(new DeleteEstimateCommand(EstimateId: Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentEstimate_ThrowsEstimateNotFoundException()
    {
        // Arrange
        var (user, _, _) = await SeedUserWithCompanyAndEstimateAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteEstimateHandler(DbContext, CurrentUserService);

        // Act & Assert
        var act = () =>
            handler.Handle(new DeleteEstimateCommand(EstimateId: Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<EstimateNotFoundException>();
    }

    [Fact]
    public async Task Handle_EstimateBelongsToOtherUser_ThrowsEstimateNotFoundException()
    {
        // Arrange — estimate1 belongs to user1
        var (_, _, estimate) = await SeedUserWithCompanyAndEstimateAsync();
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

        // Authenticate as user2 who does not own the estimate
        SetCurrentUser(user2.Id, user2.Email);
        var handler = new DeleteEstimateHandler(DbContext, CurrentUserService);

        // Act & Assert — handler searches user2's companies; estimate not found → EstimateNotFoundException
        var act = () =>
            handler.Handle(new DeleteEstimateCommand(EstimateId: estimate.Id), CancellationToken.None);
        await act.Should().ThrowAsync<EstimateNotFoundException>();
    }
}
