using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Estimate.UpdateEstimate;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Estimate.UpdateEstimate;

[Collection("Database")]
public class UpdateEstimateHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Client Client,
        Product ProductA,
        Product ProductB,
        Domain.Entities.Estimate Estimate
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
            Description = "Widget A",
            CompanyId = company.Id,
            Company = company,
        };
        var productB = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget B",
            Price = 20m,
            Description = "Widget B",
            CompanyId = company.Id,
            Company = company,
        };
        var estimate = new Domain.Entities.Estimate
        {
            Id = Guid.NewGuid(),
            EstimateNumber = "EST-0001",
            EstimateDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresOn = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = EstimateStatus.Draft,
            Notes = "Original notes",
            ClientId = client.Id,
            Client = client,
            CompanyId = company.Id,
            Company = company,
            ProductEstimates = [],
        };
        var productEstimate = new ProductEstimate
        {
            Id = Guid.NewGuid(),
            ProductId = productA.Id,
            Product = productA,
            EstimateId = estimate.Id,
            Estimate = estimate,
            CompanyId = company.Id,
            Company = company,
            Quantity = 2,
            UnitPrice = productA.Price,
        };
        estimate.ProductEstimates.Add(productEstimate);
        estimate.TotalAmount = 2 * productA.Price;

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddRangeAsync(productA, productB);
        await DbContext.Estimates.AddAsync(estimate);
        await DbContext.SaveChangesAsync();
        return (user, company, client, productA, productB, estimate);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesScalarFields()
    {
        // Arrange
        var (user, company, _, _, _, estimate) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var newDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var newExpiry = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: null,
            EstimateDate: newDate,
            ExpiresOn: newExpiry,
            Status: EstimateStatus.Sent,
            Notes: "Updated notes",
            Products: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Estimates.FindAsync(estimate.Id);
        saved.Should().NotBeNull();
        saved!.EstimateDate.Should().Be(newDate);
        saved.ExpiresOn.Should().Be(newExpiry);
        saved.Status.Should().Be(EstimateStatus.Sent);
        saved.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task Handle_UpdateProducts_ReplacesProductEstimates()
    {
        // Arrange
        var (user, company, _, productA, productB, estimate) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products:
            [
                new UpdateEstimateProductItem(productB.Id, 4),
            ]
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — old ProductEstimate replaced with new one for productB
        DbContext.ChangeTracker.Clear();
        var savedEstimate = await DbContext
            .Estimates.Include(e => e.ProductEstimates)
            .FirstOrDefaultAsync(e => e.Id == estimate.Id);
        savedEstimate.Should().NotBeNull();
        savedEstimate!.ProductEstimates.Should().HaveCount(1);
        savedEstimate.ProductEstimates.First().ProductId.Should().Be(productB.Id);
        savedEstimate.ProductEstimates.First().Quantity.Should().Be(4);
        savedEstimate.TotalAmount.Should().Be(4 * 20m);
    }

    [Fact]
    public async Task Handle_NullFields_DoesNotOverwriteExistingValues()
    {
        // Arrange
        var (user, company, _, _, _, estimate) = await SeedFullScenarioAsync();
        var originalDate = estimate.EstimateDate;
        var originalExpiry = estimate.ExpiresOn;
        var originalStatus = estimate.Status;
        var originalNotes = estimate.Notes;

        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        // Pass all nulls — nothing should change
        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Estimates.FindAsync(estimate.Id);
        saved!.EstimateDate.Should().Be(originalDate);
        saved.ExpiresOn.Should().Be(originalExpiry);
        saved.Status.Should().Be(originalStatus);
        saved.Notes.Should().Be(originalNotes);
    }

    [Fact]
    public async Task Handle_UpdateClient_ChangesClientOnEstimate()
    {
        // Arrange
        var (user, company, _, _, _, estimate) = await SeedFullScenarioAsync();

        var newClient = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            Name = "New Client",
            Email = "new-client@test.com",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Clients.AddAsync(newClient);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: newClient.Id,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Estimates.FindAsync(estimate.Id);
        saved!.ClientId.Should().Be(newClient.Id);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: Guid.NewGuid(),
            EstimateId: Guid.NewGuid(),
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
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
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: Guid.NewGuid(),
            EstimateId: Guid.NewGuid(),
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentEstimate_ThrowsEstimateNotFoundException()
    {
        // Arrange
        var (user, company, _, _, _, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: Guid.NewGuid(), // non-existent
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<EstimateNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentClient_ThrowsClientNotFoundException()
    {
        // Arrange
        var (user, company, _, _, _, estimate) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: Guid.NewGuid(), // non-existent
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var (user, company, _, _, _, estimate) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: [new UpdateEstimateProductItem(Guid.NewGuid(), 1)] // non-existent
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (_, company, _, _, _, estimate) = await SeedFullScenarioAsync();
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
        var handler = new UpdateEstimateHandler(DbContext, CurrentUserService);

        var command = new UpdateEstimateCommand(
            CompanyId: company.Id,
            EstimateId: estimate.Id,
            ClientId: null,
            EstimateDate: null,
            ExpiresOn: null,
            Status: null,
            Notes: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
