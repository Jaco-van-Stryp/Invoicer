using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Enums;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Payment.DeletePayment;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Payment.DeletePayment;

[Collection("Database")]
public class DeletePaymentHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Invoice Invoice,
        Domain.Entities.Payment Payment
    )> SeedInvoiceWithPaymentAsync(
        decimal productPrice = 100m,
        int quantity = 2,
        decimal paymentAmount = 50m
    )
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
            Status = InvoiceStatus.Partial,
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
                    Quantity = quantity,
                },
            ],
        };
        var payment = new Domain.Entities.Payment
        {
            Id = Guid.NewGuid(),
            Amount = paymentAmount,
            PaidOn = DateTime.UtcNow,
            Notes = null,
            InvoiceId = invoice.Id,
            Invoice = invoice,
            CompanyId = company.Id,
            Company = company,
        };

        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Clients.AddAsync(client);
        await DbContext.Products.AddAsync(product);
        await DbContext.Invoices.AddAsync(invoice);
        await DbContext.Payments.AddAsync(payment);
        await DbContext.SaveChangesAsync();

        return (user, company, invoice, payment);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesPaymentAndRecalculatesStatus()
    {
        // Arrange — totalDue = 200, payment = 50, so after deletion status -> Unpaid
        var (user, company, invoice, payment) = await SeedInvoiceWithPaymentAsync(100m, 2, 50m);
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeletePaymentHandler(DbContext, CurrentUserService);

        var command = new DeletePaymentCommand(company.Id, invoice.Id, payment.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var deletedPayment = await DbContext.Payments.FindAsync(payment.Id);
        deletedPayment.Should().BeNull();

        var updatedInvoice = await DbContext.Invoices.FindAsync(invoice.Id);
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Unpaid);
    }

    [Fact]
    public async Task Handle_DeletingPaymentThatMadeInvoicePaid_StatusRevertsToPartial()
    {
        // Arrange — totalDue = 100, two payments: 50 + 50 = 100 (Paid)
        var (user, company, invoice, _) = await SeedInvoiceWithPaymentAsync(50m, 2, 50m);

        // Add a second payment that makes it fully paid
        var secondPayment = new Domain.Entities.Payment
        {
            Id = Guid.NewGuid(),
            Amount = 50m,
            PaidOn = DateTime.UtcNow,
            Notes = null,
            InvoiceId = invoice.Id,
            Invoice = null!,
            CompanyId = company.Id,
            Company = null!,
        };
        await DbContext.Payments.AddAsync(secondPayment);
        invoice.Status = InvoiceStatus.Paid;
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new DeletePaymentHandler(DbContext, CurrentUserService);

        // Act — delete the second payment (50 remains, total due = 100)
        await handler.Handle(
            new DeletePaymentCommand(company.Id, invoice.Id, secondPayment.Id),
            CancellationToken.None
        );

        // Assert
        DbContext.ChangeTracker.Clear();
        var updatedInvoice = await DbContext.Invoices.FindAsync(invoice.Id);
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Partial);
    }

    [Fact]
    public async Task Handle_NonExistentPayment_ThrowsPaymentNotFoundException()
    {
        var (user, company, invoice, _) = await SeedInvoiceWithPaymentAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeletePaymentHandler(DbContext, CurrentUserService);

        var act = () =>
            handler.Handle(
                new DeletePaymentCommand(company.Id, invoice.Id, Guid.NewGuid()),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        SetCurrentUser(Guid.NewGuid());
        var handler = new DeletePaymentHandler(DbContext, CurrentUserService);

        var act = () =>
            handler.Handle(
                new DeletePaymentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentCompany_ThrowsCompanyNotFoundException()
    {
        var (_, _, invoice, payment) = await SeedInvoiceWithPaymentAsync();
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
        var handler = new DeletePaymentHandler(DbContext, CurrentUserService);

        var act = () =>
            handler.Handle(
                new DeletePaymentCommand(Guid.NewGuid(), invoice.Id, payment.Id),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentInvoice_ThrowsInvoiceNotFoundException()
    {
        var (user, company, _, payment) = await SeedInvoiceWithPaymentAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeletePaymentHandler(DbContext, CurrentUserService);

        var act = () =>
            handler.Handle(
                new DeletePaymentCommand(company.Id, Guid.NewGuid(), payment.Id),
                CancellationToken.None
            );

        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }
}
