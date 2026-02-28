using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Payment.RecordPayment;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Invoicer.Tests.Features.Payment.RecordPayment;

[Collection("Database")]
public class RecordPaymentHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Client Client,
        Domain.Entities.Invoice Invoice
    )> SeedFullScenarioAsync(decimal productPrice = 100m, int quantity = 2)
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
            Name = "Test Product",
            Description = "A test product",
            Price = productPrice,
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
                    InvoiceId = Guid.Empty, // set by EF
                    Invoice = null!,
                    CompanyId = company.Id,
                    Company = company,
                    Quantity = quantity,
                },
            ],
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.SaveChangesAsync();

        return (user, company, client, invoice);
    }

    [Fact]
    public async Task Handle_PartialPayment_SetsStatusToPartial()
    {
        // Arrange — total due = 100 * 2 = 200
        var (user, company, _, invoice) = await SeedFullScenarioAsync(100m, 2);
        SetCurrentUser(user.Id, user.Email);
        var handler = new RecordPaymentHandler(DbContext, CurrentUserService, Substitute.For<IEmailService>(), Substitute.For<IEmailTemplateService>());

        var command = new RecordPaymentCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            Amount: 50m,
            PaidOn: DateTime.UtcNow,
            Notes: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewInvoiceStatus.Should().Be(InvoiceStatus.Partial);
        result.Amount.Should().Be(50m);
        result.PaymentId.Should().NotBeEmpty();

        DbContext.ChangeTracker.Clear();
        var savedInvoice = await DbContext.Invoices.FindAsync(invoice.Id);
        savedInvoice!.Status.Should().Be(InvoiceStatus.Partial);

        var savedPayment = await DbContext.Payments.FindAsync(result.PaymentId);
        savedPayment.Should().NotBeNull();
        savedPayment!.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_FullPayment_SetsStatusToPaid()
    {
        // Arrange — total due = 100 * 2 = 200
        var (user, company, _, invoice) = await SeedFullScenarioAsync(100m, 2);
        SetCurrentUser(user.Id, user.Email);
        var handler = new RecordPaymentHandler(DbContext, CurrentUserService, Substitute.For<IEmailService>(), Substitute.For<IEmailTemplateService>());

        var command = new RecordPaymentCommand(
            CompanyId: company.Id,
            InvoiceId: invoice.Id,
            Amount: 200m,
            PaidOn: DateTime.UtcNow,
            Notes: "Paid in full"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.NewInvoiceStatus.Should().Be(InvoiceStatus.Paid);

        DbContext.ChangeTracker.Clear();
        var savedInvoice = await DbContext.Invoices.FindAsync(invoice.Id);
        savedInvoice!.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task Handle_TwoPartialPaymentsEqualingTotal_SetsStatusToPaid()
    {
        // Arrange — total due = 100 * 2 = 200
        var (user, company, _, invoice) = await SeedFullScenarioAsync(100m, 2);
        SetCurrentUser(user.Id, user.Email);
        var handler = new RecordPaymentHandler(DbContext, CurrentUserService, Substitute.For<IEmailService>(), Substitute.For<IEmailTemplateService>());

        await handler.Handle(
            new RecordPaymentCommand(company.Id, invoice.Id, 100m, DateTime.UtcNow, null),
            CancellationToken.None
        );

        // Act — second payment completes it
        var result = await handler.Handle(
            new RecordPaymentCommand(company.Id, invoice.Id, 100m, DateTime.UtcNow, null),
            CancellationToken.None
        );

        // Assert
        result.NewInvoiceStatus.Should().Be(InvoiceStatus.Paid);

        DbContext.ChangeTracker.Clear();
        var savedInvoice = await DbContext.Invoices.FindAsync(invoice.Id);
        savedInvoice!.Status.Should().Be(InvoiceStatus.Paid);

        var payments = await DbContext.Payments.Where(p => p.InvoiceId == invoice.Id).ToListAsync();
        payments.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        SetCurrentUser(Guid.NewGuid());
        var handler = new RecordPaymentHandler(DbContext, CurrentUserService, Substitute.For<IEmailService>(), Substitute.For<IEmailTemplateService>());

        var act = () =>
            handler.Handle(
                new RecordPaymentCommand(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    100m,
                    DateTime.UtcNow,
                    null
                ),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        var (_, company, _, invoice) = await SeedFullScenarioAsync();
        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@test.com",
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };
        await DbContext.Users.AddAsync(otherUser);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(otherUser.Id, otherUser.Email);
        var handler = new RecordPaymentHandler(DbContext, CurrentUserService, Substitute.For<IEmailService>(), Substitute.For<IEmailTemplateService>());

        var act = () =>
            handler.Handle(
                new RecordPaymentCommand(company.Id, invoice.Id, 100m, DateTime.UtcNow, null),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentInvoice_ThrowsInvoiceNotFoundException()
    {
        var (user, company, _, _) = await SeedFullScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new RecordPaymentHandler(DbContext, CurrentUserService, Substitute.For<IEmailService>(), Substitute.For<IEmailTemplateService>());

        var act = () =>
            handler.Handle(
                new RecordPaymentCommand(company.Id, Guid.NewGuid(), 100m, DateTime.UtcNow, null),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }
}
