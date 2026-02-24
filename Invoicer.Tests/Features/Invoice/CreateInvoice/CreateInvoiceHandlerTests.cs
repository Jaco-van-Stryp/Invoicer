using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.CreateInvoice;
using Invoicer.Tests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Invoicer.Tests.Features.Invoice.CreateInvoice;

[Collection("Database")]
public class CreateInvoiceHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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
    public async Task Handle_ValidCommand_CreatesInvoiceWithProducts()
    {
        // Arrange
        var (user, company, client, productA, productB) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var invoiceDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoiceDue = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);

        var command = new CreateInvoiceCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            InvoiceDate: invoiceDate,
            InvoiceDue: invoiceDue,
            Products:
            [
                new CreateInvoiceProductItem(productA.Id, 2),
                new CreateInvoiceProductItem(productB.Id, 5),
            ]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — verify response
        result.Id.Should().NotBeEmpty();
        result.InvoiceNumber.Should().Be("INV-0001");

        // Assert — verify persistence
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.InvoiceNumber.Should().Be("INV-0001");
        saved.InvoiceDate.Should().Be(invoiceDate);
        saved.InvoiceDue.Should().Be(invoiceDue);
        saved.ClientId.Should().Be(client.Id);
        saved.CompanyId.Should().Be(company.Id);
        saved.Products.Should().HaveCount(2);

        var piA = saved.Products.First(p => p.ProductId == productA.Id);
        piA.Quantity.Should().Be(2);
        piA.CompanyId.Should().Be(company.Id);

        var piB = saved.Products.First(p => p.ProductId == productB.Id);
        piB.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task Handle_SingleProduct_CreatesInvoiceSuccessfully()
    {
        // Arrange
        var (user, company, client, productA, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(productA.Id, 1)]
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.InvoiceNumber.Should().Be("INV-0001");
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.Products.Should().HaveCount(1);
        saved.Products.First().ProductId.Should().Be(productA.Id);
        saved.Products.First().Quantity.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MultipleInvoices_IncrementsInvoiceNumber()
    {
        // Arrange
        var (user, company, client, productA, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(productA.Id, 1)]
        );

        // Act
        var result1 = await handler.Handle(command, CancellationToken.None);
        var result2 = await handler.Handle(command, CancellationToken.None);

        // Assert
        result1.InvoiceNumber.Should().Be("INV-0001");
        result2.InvoiceNumber.Should().Be("INV-0002");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(Guid.NewGuid(), 1)]
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
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(Guid.NewGuid(), 1)]
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
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: company.Id,
            ClientId: Guid.NewGuid(), // non-existent client
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(productA.Id, 1)]
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
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(Guid.NewGuid(), 1)] // non-existent product
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (user1, company, client, productA, _) = await SeedFullScenarioAsync();
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
        var handler = new CreateInvoiceHandler(DbContext, CurrentUserService, Substitute.For<ISender>());

        var command = new CreateInvoiceCommand(
            CompanyId: company.Id,
            ClientId: client.Id,
            InvoiceDate: DateTime.UtcNow,
            InvoiceDue: DateTime.UtcNow.AddDays(30),
            Products: [new CreateInvoiceProductItem(productA.Id, 1)]
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
