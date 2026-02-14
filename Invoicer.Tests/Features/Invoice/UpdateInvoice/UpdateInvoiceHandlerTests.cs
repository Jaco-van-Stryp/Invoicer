using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.UpdateInvoice;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Invoice.UpdateInvoice;

[Collection("Database")]
public class UpdateInvoiceHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Client Client,
        Product ProductA,
        Product ProductB,
        Domain.Entities.Invoice Invoice
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

        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-ORIG",
            InvoiceDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            InvoiceDue = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            ClientId = client.Id,
            Client = client,
            CompanyId = company.Id,
            Company = company,
            Products = new List<ProductInvoice>(),
        };
        var productInvoice = new ProductInvoice
        {
            Id = Guid.NewGuid(),
            ProductId = productA.Id,
            Product = productA,
            InvoiceId = invoice.Id,
            Invoice = invoice,
            CompanyId = company.Id,
            Company = company,
            Quantity = 2,
        };
        invoice.Products.Add(productInvoice);

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddRangeAsync(productA, productB);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();
        return (user, company, client, productA, productB, invoice);
    }

    [Fact]
    public async Task Handle_AllFieldsProvided_UpdatesAllFields()
    {
        // Arrange
        var (user, company, client, productA, productB, invoice) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var newDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDue = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: "INV-UPD",
            InvoiceDate: newDate,
            InvoiceDue: newDue,
            ClientId: null,
            Products: [new UpdateInvoiceProductItem(productB.Id, 10)]
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        saved.Should().NotBeNull();
        saved!.InvoiceNumber.Should().Be("INV-UPD");
        saved.InvoiceDate.Should().Be(newDate);
        saved.InvoiceDue.Should().Be(newDue);
        saved.Products.Should().HaveCount(1);
        saved.Products.First().ProductId.Should().Be(productB.Id);
        saved.Products.First().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task Handle_UpdateClientId_UpdatesClient()
    {
        // Arrange
        var (user, company, client, productA, productB, invoice) = await SeedFullScenarioAsync();

        var secondClient = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            Name = "Second Client",
            Email = "second@test.com",
            Address = "789 Second St",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Clients.AddAsync(secondClient);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var newDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var newDue = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: "INV-CLIENT-UPD",
            InvoiceDate: newDate,
            InvoiceDue: newDue,
            ClientId: secondClient.Id,
            Products: [new UpdateInvoiceProductItem(productB.Id, 5)]
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        saved.Should().NotBeNull();
        saved!.ClientId.Should().Be(secondClient.Id);
        saved.InvoiceNumber.Should().Be("INV-CLIENT-UPD");
        saved.InvoiceDate.Should().Be(newDate);
        saved.InvoiceDue.Should().Be(newDue);
        saved.Products.Should().HaveCount(1);
        saved.Products.First().ProductId.Should().Be(productB.Id);
        saved.Products.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task Handle_OnlyInvoiceNumberProvided_UpdatesOnlyInvoiceNumber()
    {
        // Arrange
        var (user, company, _, _, _, invoice) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: "INV-RENAMED",
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        saved.Should().NotBeNull();
        saved!.InvoiceNumber.Should().Be("INV-RENAMED");
        saved.InvoiceDate.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        saved.InvoiceDue.Should().Be(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));
        // Products should remain unchanged when null
        saved.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ProductsReplaced_OldProductInvoicesRemoved()
    {
        // Arrange
        var (user, company, _, productA, productB, invoice) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products:
            [
                new UpdateInvoiceProductItem(productA.Id, 7),
                new UpdateInvoiceProductItem(productB.Id, 3),
            ]
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        saved.Should().NotBeNull();
        saved!.Products.Should().HaveCount(2);
        saved.Products.First(p => p.ProductId == productA.Id).Quantity.Should().Be(7);
        saved.Products.First(p => p.ProductId == productB.Id).Quantity.Should().Be(3);

        // Old product invoices should have been removed
        var allPIs = await DbContext
            .ProductInvoices.Where(pi => pi.InvoiceId == invoice.Id)
            .ToListAsync();
        allPIs.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_NothingChanges()
    {
        // Arrange
        var (user, company, _, _, _, invoice) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext
            .Invoices.Include(i => i.Products)
            .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        saved.Should().NotBeNull();
        saved!.InvoiceNumber.Should().Be("INV-ORIG");
        saved.InvoiceDate.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        saved.InvoiceDue.Should().Be(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));
        saved.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_UpdateClientId_ValidatesClientExists()
    {
        // Arrange
        var (user, company, _, _, _, invoice) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: Guid.NewGuid(), // non-existent client
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ClientNotFoundException>();
    }

    [Fact]
    public async Task Handle_UpdateWithNonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var (user, company, _, _, _, invoice) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: null,
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products: [new UpdateInvoiceProductItem(Guid.NewGuid(), 1)]
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: Guid.NewGuid(),
            InvoiceId: Guid.NewGuid(),
            InvoiceNumber: "X",
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
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
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: Guid.NewGuid(),
            InvoiceId: Guid.NewGuid(),
            InvoiceNumber: "X",
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentInvoice_ThrowsInvoiceNotFoundException()
    {
        // Arrange
        var (user, company, _, _, _, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: Guid.NewGuid(),
            InvoiceNumber: "X",
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (user1, company, _, _, _, invoice) = await SeedFullScenarioAsync();
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
        var handler = new UpdateInvoiceHandler(DbContext, CurrentUserService);

        var command = new UpdateInvoiceCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            InvoiceNumber: "Hijacked",
            InvoiceDate: null,
            InvoiceDue: null,
            ClientId: null,
            Products: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
