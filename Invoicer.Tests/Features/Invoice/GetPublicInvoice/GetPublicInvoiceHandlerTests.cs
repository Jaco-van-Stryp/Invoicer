using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.GetPublicInvoice;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Invoice.GetPublicInvoice;

[Collection("Database")]
public class GetPublicInvoiceHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        Domain.Entities.Company Company,
        Domain.Entities.Client Client,
        Product Product,
        Domain.Entities.Invoice Invoice
    )> SeedInvoiceScenarioAsync(decimal productPrice = 50m, int quantity = 2)
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
            PhoneNumber = "555-9999",
            CompanyId = company.Id,
            Company = company,
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = productPrice,
            Description = "A widget",
            CompanyId = company.Id,
            Company = company,
        };
        var invoiceDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoiceDue = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-0001",
            InvoiceDate = invoiceDate,
            InvoiceDue = invoiceDue,
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
            Quantity = quantity,
        };
        invoice.Products.Add(productInvoice);

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();
        return (company, client, product, invoice);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsInvoiceDetails()
    {
        // Arrange
        var (company, client, product, invoice) = await SeedInvoiceScenarioAsync(50m, 2);
        var handler = new GetPublicInvoiceHandler(DbContext);

        // Act
        var result = await handler.Handle(new GetPublicInvoiceQuery(invoice.Id), CancellationToken.None);

        // Assert
        result.Id.Should().Be(invoice.Id);
        result.InvoiceNumber.Should().Be("INV-0001");
        result.InvoiceDate.Should().Be(invoice.InvoiceDate);
        result.DueDate.Should().Be(invoice.InvoiceDue);
        result.Status.Should().Be("Unpaid");

        result.Company.Name.Should().Be(company.Name);
        result.Company.Email.Should().Be(company.Email);

        result.Client.Name.Should().Be(client.Name);
        result.Client.Email.Should().Be(client.Email);

        result.Products.Should().HaveCount(1);
        result.Products[0].Name.Should().Be(product.Name);
        result.Products[0].Quantity.Should().Be(2);
        result.Products[0].UnitPrice.Should().Be(50m);
        result.Products[0].TotalPrice.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_CalculatesCorrectTotalAmount()
    {
        // Arrange — price=30, quantity=4 → total=120
        var (_, _, _, invoice) = await SeedInvoiceScenarioAsync(30m, 4);
        var handler = new GetPublicInvoiceHandler(DbContext);

        // Act
        var result = await handler.Handle(new GetPublicInvoiceQuery(invoice.Id), CancellationToken.None);

        // Assert
        result.TotalAmount.Should().Be(120m);
    }

    [Fact]
    public async Task Handle_NonExistentInvoice_ThrowsInvoiceNotFoundException()
    {
        // Arrange
        var handler = new GetPublicInvoiceHandler(DbContext);

        // Act & Assert
        var act = () => handler.Handle(new GetPublicInvoiceQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }
}
