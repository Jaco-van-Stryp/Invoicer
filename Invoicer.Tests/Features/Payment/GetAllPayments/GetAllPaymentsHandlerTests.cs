using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Payment.GetAllPayments;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Payment.GetAllPayments;

[Collection("Database")]
public class GetAllPaymentsHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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
            CompanyId = company.Id,
            Company = company,
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Widget",
            Price = 100m,
            Description = "A widget",
            CompanyId = company.Id,
            Company = company,
        };
        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-0001",
            InvoiceDate = DateTime.UtcNow,
            InvoiceDue = DateTime.UtcNow.AddDays(30),
            ClientId = client.Id,
            Client = client,
            CompanyId = company.Id,
            Company = company,
            Products =
            [
                new ProductInvoice
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    InvoiceId = Guid.Empty,
                    Invoice = null!,
                    CompanyId = company.Id,
                    Company = company,
                    Quantity = 2,
                },
            ],
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();
        return (user, company, invoice);
    }

    private async Task<Domain.Entities.Payment> SeedPaymentAsync(
        Domain.Entities.Company company,
        Domain.Entities.Invoice invoice,
        decimal amount,
        DateTime paidOn,
        string? notes = null
    )
    {
        var payment = new Domain.Entities.Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            PaidOn = paidOn,
            Notes = notes,
            InvoiceId = invoice.Id,
            Invoice = invoice,
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Payments.AddAsync(payment);
        await DbContext.SaveChangesAsync();
        return payment;
    }

    [Fact]
    public async Task Handle_CompanyWithPayments_ReturnsAllPayments()
    {
        // Arrange
        var (user, company, invoice) = await SeedUserWithCompanyAndInvoiceAsync();
        await SeedPaymentAsync(company, invoice, 50m, new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc));
        await SeedPaymentAsync(company, invoice, 75m, new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc));

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllPaymentsQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Amount).Should().Contain([50m, 75m]);
    }

    [Fact]
    public async Task Handle_CompanyWithNoPayments_ReturnsEmptyList()
    {
        // Arrange
        var (user, company, _) = await SeedUserWithCompanyAndInvoiceAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllPaymentsQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsPaymentFieldsIncludingInvoiceAndClient()
    {
        // Arrange
        var (user, company, invoice) = await SeedUserWithCompanyAndInvoiceAsync();
        var paidOn = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var payment = await SeedPaymentAsync(company, invoice, 100m, paidOn, "Test note");

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllPaymentsQuery(company.Id), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var returned = result[0];
        returned.Id.Should().Be(payment.Id);
        returned.Amount.Should().Be(100m);
        returned.PaidOn.Should().Be(paidOn);
        returned.Notes.Should().Be("Test note");
        returned.InvoiceId.Should().Be(invoice.Id);
        returned.InvoiceNumber.Should().Be("INV-0001");
        returned.ClientName.Should().Be("Test Client");
    }

    [Fact]
    public async Task Handle_PaymentsOrderedByPaidOnDescending()
    {
        // Arrange
        var (user, company, invoice) = await SeedUserWithCompanyAndInvoiceAsync();
        var early = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var late = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        await SeedPaymentAsync(company, invoice, 10m, early);
        await SeedPaymentAsync(company, invoice, 20m, late);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act
        var result = await handler.Handle(new GetAllPaymentsQuery(company.Id), CancellationToken.None);

        // Assert — most recent first
        result[0].Amount.Should().Be(20m);
        result[1].Amount.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_PaymentsFromOtherCompany_NotReturned()
    {
        // Arrange
        var (user, company1, invoice1) = await SeedUserWithCompanyAndInvoiceAsync();

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
            User = await DbContext.Users.FindAsync(user.Id) ?? user,
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
            Price = 50m,
            Description = "Other widget",
            CompanyId = company2.Id,
            Company = company2,
        };
        var invoice2 = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-OTHER",
            InvoiceDate = DateTime.UtcNow,
            InvoiceDue = DateTime.UtcNow.AddDays(30),
            ClientId = client2.Id,
            Client = client2,
            CompanyId = company2.Id,
            Company = company2,
            Products = new List<ProductInvoice>(),
        };
        await DbContext.Companies.AddAsync(company2);
        await DbContext.Clients.AddAsync(client2);
        await DbContext.Products.AddAsync(product2);
        await DbContext.Invoices.AddAsync(invoice2);
        await DbContext.SaveChangesAsync();

        await SeedPaymentAsync(company1, invoice1, 50m, DateTime.UtcNow);
        await SeedPaymentAsync(company2, invoice2, 99m, DateTime.UtcNow);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act — query only company1
        var result = await handler.Handle(new GetAllPaymentsQuery(company1.Id), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Amount.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act & Assert
        var act = () => handler.Handle(new GetAllPaymentsQuery(Guid.NewGuid()), CancellationToken.None);
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
        var handler = new GetAllPaymentsHandler(DbContext, CurrentUserService);

        // Act & Assert
        var act = () => handler.Handle(new GetAllPaymentsQuery(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
