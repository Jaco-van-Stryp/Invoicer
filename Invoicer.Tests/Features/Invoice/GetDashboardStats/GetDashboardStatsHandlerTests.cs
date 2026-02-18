using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.GetDashboardStats;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Invoice.GetDashboardStats;

[Collection("Database")]
public class GetDashboardStatsHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Client Client1,
        Client Client2,
        Product Product
    )> SeedBaseScenarioAsync()
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
        var client1 = new Client
        {
            Id = Guid.NewGuid(),
            Name = "Client Alpha",
            Email = "alpha@test.com",
            CompanyId = company.Id,
            Company = company,
        };
        var client2 = new Client
        {
            Id = Guid.NewGuid(),
            Name = "Client Beta",
            Email = "beta@test.com",
            CompanyId = company.Id,
            Company = company,
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "A test product",
            Price = 100m,
            CompanyId = company.Id,
            Company = company,
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client1);
        await DbContext.Clients.AddAsync(client2);
        await DbContext.Products.AddAsync(product);
        await DbContext.SaveChangesAsync();

        return (user, company, client1, client2, product);
    }

    private async Task<Domain.Entities.Invoice> CreateInvoiceAsync(
        Domain.Entities.Company company,
        Client client,
        Product product,
        InvoiceStatus status = InvoiceStatus.Unpaid
    )
    {
        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            InvoiceDate = DateTime.UtcNow,
            InvoiceDue = DateTime.UtcNow.AddDays(30),
            Status = status,
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
                    Quantity = 1,
                },
            ],
        };
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();
        return invoice;
    }

    private async Task<Domain.Entities.Payment> CreatePaymentAsync(
        Domain.Entities.Invoice invoice,
        Domain.Entities.Company company,
        decimal amount,
        DateTime paidOn
    )
    {
        var payment = new Domain.Entities.Payment
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            PaidOn = paidOn,
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
    public async Task Handle_NoPayments_ReturnsEmptyMonthlyAndClientIncome()
    {
        var (user, company, _, _, _) = await SeedBaseScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var result = await handler.Handle(
            new GetDashboardStatsQuery(company.Id),
            CancellationToken.None
        );

        result.MonthlyIncome.Should().BeEmpty();
        result.IncomeByClient.Should().BeEmpty();
        result.StatusSummary.PaidCount.Should().Be(0);
        result.StatusSummary.PartialCount.Should().Be(0);
        result.StatusSummary.UnpaidCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithPayments_ReturnsCorrectMonthlyGrouping()
    {
        var (user, company, client1, _, product) = await SeedBaseScenarioAsync();
        var invoice = await CreateInvoiceAsync(company, client1, product);

        // Two payments in January 2025 and one in February 2025
        await CreatePaymentAsync(
            invoice,
            company,
            100m,
            new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        );
        await CreatePaymentAsync(
            invoice,
            company,
            200m,
            new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc)
        );
        await CreatePaymentAsync(
            invoice,
            company,
            300m,
            new DateTime(2025, 2, 5, 0, 0, 0, DateTimeKind.Utc)
        );

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var result = await handler.Handle(
            new GetDashboardStatsQuery(company.Id),
            CancellationToken.None
        );

        result.MonthlyIncome.Should().HaveCount(2);

        var jan = result.MonthlyIncome.First(m => m.Month == 1);
        jan.Year.Should().Be(2025);
        jan.TotalPaid.Should().Be(300m);

        var feb = result.MonthlyIncome.First(m => m.Month == 2);
        feb.Year.Should().Be(2025);
        feb.TotalPaid.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_WithPayments_ReturnsCorrectClientIncomeBreakdown()
    {
        var (user, company, client1, client2, product) = await SeedBaseScenarioAsync();
        var invoiceA = await CreateInvoiceAsync(company, client1, product);
        var invoiceB = await CreateInvoiceAsync(company, client2, product);

        await CreatePaymentAsync(invoiceA, company, 400m, DateTime.UtcNow);
        await CreatePaymentAsync(invoiceB, company, 150m, DateTime.UtcNow);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var result = await handler.Handle(
            new GetDashboardStatsQuery(company.Id),
            CancellationToken.None
        );

        result.IncomeByClient.Should().HaveCount(2);

        var alpha = result.IncomeByClient.First(c => c.ClientName == "Client Alpha");
        alpha.TotalPaid.Should().Be(400m);

        var beta = result.IncomeByClient.First(c => c.ClientName == "Client Beta");
        beta.TotalPaid.Should().Be(150m);

        // Results ordered by descending total
        result.IncomeByClient.First().ClientName.Should().Be("Client Alpha");
    }

    [Fact]
    public async Task Handle_InvoiceStatusSummary_CountsCorrectly()
    {
        var (user, company, client1, client2, product) = await SeedBaseScenarioAsync();
        await CreateInvoiceAsync(company, client1, product, InvoiceStatus.Paid);
        await CreateInvoiceAsync(company, client1, product, InvoiceStatus.Paid);
        await CreateInvoiceAsync(company, client2, product, InvoiceStatus.Partial);
        await CreateInvoiceAsync(company, client2, product, InvoiceStatus.Unpaid);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var result = await handler.Handle(
            new GetDashboardStatsQuery(company.Id),
            CancellationToken.None
        );

        result.StatusSummary.PaidCount.Should().Be(2);
        result.StatusSummary.PartialCount.Should().Be(1);
        result.StatusSummary.UnpaidCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PaymentsFromOtherCompany_NotIncludedInStats()
    {
        var (user, company, client1, _, product) = await SeedBaseScenarioAsync();

        // Create a second company with its own invoice and payment
        var otherCompany = new Domain.Entities.Company
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
            User = null!,
        };
        await DbContext.Companies.AddAsync(otherCompany);

        var otherClient = new Client
        {
            Id = Guid.NewGuid(),
            Name = "Other Client",
            Email = "other@client.com",
            CompanyId = otherCompany.Id,
            Company = otherCompany,
        };
        await DbContext.Clients.AddAsync(otherClient);
        await DbContext.SaveChangesAsync();

        var otherInvoice = await CreateInvoiceAsync(otherCompany, otherClient, product);
        await CreatePaymentAsync(otherInvoice, otherCompany, 999m, DateTime.UtcNow);

        // Also add a payment for the original company
        var invoice = await CreateInvoiceAsync(company, client1, product);
        await CreatePaymentAsync(invoice, company, 100m, DateTime.UtcNow);

        SetCurrentUser(user.Id, user.Email);
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var result = await handler.Handle(
            new GetDashboardStatsQuery(company.Id),
            CancellationToken.None
        );

        result.MonthlyIncome.Should().HaveCount(1);
        result.MonthlyIncome.First().TotalPaid.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        SetCurrentUser(Guid.NewGuid());
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var act = () =>
            handler.Handle(new GetDashboardStatsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentCompany_ThrowsCompanyNotFoundException()
    {
        var (user, _, _, _, _) = await SeedBaseScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new GetDashboardStatsHandler(DbContext, CurrentUserService);

        var act = () =>
            handler.Handle(new GetDashboardStatsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
