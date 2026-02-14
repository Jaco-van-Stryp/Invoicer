using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.DeleteInvoice;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Invoice.DeleteInvoice;

[Collection("Database")]
public class DeleteInvoiceHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Invoice Invoice
    )> SeedUserWithCompanyAndInvoiceAsync()
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
        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-DOOMED",
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
            ProductId = product.Id,
            Product = product,
            InvoiceId = invoice.Id,
            Invoice = invoice,
            CompanyId = company.Id,
            Company = company,
            Quantity = 3,
        };
        invoice.Products.Add(productInvoice);

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();
        return (user, company, invoice);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesInvoiceAndProductInvoices()
    {
        // Arrange
        var (user, company, invoice) = await SeedUserWithCompanyAndInvoiceAsync();
        var invoiceId = invoice.Id;
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteInvoiceHandler(DbContext, CurrentUserService);

        var command = new DeleteInvoiceCommand(CompanyId: company.Id, InvoiceId: invoiceId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — invoice is deleted
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Invoices.FindAsync(invoiceId);
        deleted.Should().BeNull("the invoice should have been removed from the database");

        // Assert — product invoices are also deleted
        var productInvoices = await DbContext
            .ProductInvoices.Where(pi => pi.InvoiceId == invoiceId)
            .ToListAsync();
        productInvoices.Should().BeEmpty("product-invoice join records should also be removed");
    }

    [Fact]
    public async Task Handle_DeleteOneInvoice_OtherInvoicesRemain()
    {
        // Arrange
        var (user, company, invoice1) = await SeedUserWithCompanyAndInvoiceAsync();

        // Seed a second invoice (reuse existing client and product)
        var client = await DbContext.Clients.FirstAsync(c => c.CompanyId == company.Id);
        var product = await DbContext.Products.FirstAsync(p => p.CompanyId == company.Id);

        var invoice2 = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-SURVIVOR",
            InvoiceDate = DateTime.UtcNow,
            InvoiceDue = DateTime.UtcNow.AddDays(30),
            ClientId = client.Id,
            Client = client,
            CompanyId = company.Id,
            Company = company,
            Products = new List<ProductInvoice>(),
        };
        var pi2 = new ProductInvoice
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Product = product,
            InvoiceId = invoice2.Id,
            Invoice = invoice2,
            CompanyId = company.Id,
            Company = company,
            Quantity = 1,
        };
        invoice2.Products.Add(pi2);
        await DbContext.Invoices.AddAsync(invoice2);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteInvoiceHandler(DbContext, CurrentUserService);

        // Act — delete only the first invoice
        await handler.Handle(
            new DeleteInvoiceCommand(company.Id, invoice1.Id),
            CancellationToken.None
        );

        // Assert
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Invoices.FindAsync(invoice1.Id);
        deleted.Should().BeNull();

        var remaining = await DbContext.Invoices.FindAsync(invoice2.Id);
        remaining.Should().NotBeNull();
        remaining!.InvoiceNumber.Should().Be("INV-SURVIVOR");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new DeleteInvoiceHandler(DbContext, CurrentUserService);

        var command = new DeleteInvoiceCommand(
            CompanyId: Guid.NewGuid(),
            InvoiceId: Guid.NewGuid()
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
        var handler = new DeleteInvoiceHandler(DbContext, CurrentUserService);

        var command = new DeleteInvoiceCommand(
            CompanyId: Guid.NewGuid(),
            InvoiceId: Guid.NewGuid()
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentInvoice_ThrowsInvoiceNotFoundException()
    {
        // Arrange
        var (user, company, _) = await SeedUserWithCompanyAndInvoiceAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteInvoiceHandler(DbContext, CurrentUserService);

        var command = new DeleteInvoiceCommand(CompanyId: company.Id, InvoiceId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (user1, company, invoice) = await SeedUserWithCompanyAndInvoiceAsync();
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
        var handler = new DeleteInvoiceHandler(DbContext, CurrentUserService);

        var command = new DeleteInvoiceCommand(CompanyId: company.Id, InvoiceId: invoice.Id);

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
