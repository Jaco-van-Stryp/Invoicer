using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.GetAllInvoices;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Invoice.GetAllInvoices;

[Collection("Database")]
public class GetAllInvoicesHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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

    private async Task<Domain.Entities.Invoice> SeedInvoiceAsync(
        Domain.Entities.Company company,
        Domain.Entities.Client client,
        Product product,
        string invoiceNumber,
        int quantity
    )
    {
        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
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
            Quantity = quantity,
        };
        invoice.Products.Add(productInvoice);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task Handle_CompanyWithInvoices_ReturnsAllInvoices()
    {
        // Arrange
        var (user, company, client, product) = await SeedUserWithCompanyClientAndProductAsync();
        await SeedInvoiceAsync(company, client, product, "INV-001", 2);
        await SeedInvoiceAsync(company, client, product, "INV-002", 5);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllInvoicesHandler(DbContext, CurrentUserService);

        var query = new GetAllInvoicesQuery(CompanyId: company.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(i => i.InvoiceNumber).Should().Contain(["INV-001", "INV-002"]);
    }

    [Fact]
    public async Task Handle_CompanyWithNoInvoices_ReturnsEmptyList()
    {
        // Arrange
        var (user, company, _, _) = await SeedUserWithCompanyClientAndProductAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllInvoicesHandler(DbContext, CurrentUserService);

        var query = new GetAllInvoicesQuery(CompanyId: company.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAllInvoiceFieldsIncludingProducts()
    {
        // Arrange
        var (user, company, client, product) = await SeedUserWithCompanyClientAndProductAsync();
        var invoice = await SeedInvoiceAsync(company, client, product, "INV-FULL", 3);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllInvoicesHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(
            new GetAllInvoicesQuery(company.Id),
            CancellationToken.None
        );

        // Assert
        result.Should().HaveCount(1);
        var returned = result[0];
        returned.Id.Should().Be(invoice.Id);
        returned.InvoiceNumber.Should().Be("INV-FULL");
        returned.InvoiceDate.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        returned.InvoiceDue.Should().Be(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc));
        returned.ClientId.Should().Be(client.Id);
        returned.ClientName.Should().Be("Test Client");

        returned.Products.Should().HaveCount(1);
        var returnedProduct = returned.Products[0];
        returnedProduct.ProductId.Should().Be(product.Id);
        returnedProduct.ProductName.Should().Be("Widget");
        returnedProduct.Price.Should().Be(15m);
        returnedProduct.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Handle_InvoicesFromOtherCompany_NotReturned()
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

        await SeedInvoiceAsync(company1, client1, product1, "INV-C1", 1);
        await SeedInvoiceAsync(company2, client2, product2, "INV-C2", 1);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllInvoicesHandler(DbContext, CurrentUserService);

        // Act — query only company1
        var result = await handler.Handle(
            new GetAllInvoicesQuery(CompanyId: company1.Id),
            CancellationToken.None
        );

        // Assert
        result.Should().HaveCount(1);
        result[0].InvoiceNumber.Should().Be("INV-C1");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new GetAllInvoicesHandler(DbContext, CurrentUserService);

        var query = new GetAllInvoicesQuery(CompanyId: Guid.NewGuid());

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
        var handler = new GetAllInvoicesHandler(DbContext, CurrentUserService);

        var query = new GetAllInvoicesQuery(CompanyId: Guid.NewGuid());

        // Act & Assert
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
