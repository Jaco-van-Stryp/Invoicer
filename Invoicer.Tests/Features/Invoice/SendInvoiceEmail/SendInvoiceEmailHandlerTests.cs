using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Invoice.SendInvoiceEmail;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;
using Invoicer.Tests.Infrastructure;
using NSubstitute;

namespace Invoicer.Tests.Features.Invoice.SendInvoiceEmail;

[Collection("Database")]
public class SendInvoiceEmailHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Domain.Entities.Invoice Invoice
    )> SeedInvoiceScenarioAsync()
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
            Price = 100m,
            Description = "A widget",
            CompanyId = company.Id,
            Company = company,
        };
        var invoice = new Domain.Entities.Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-0001",
            InvoiceDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            InvoiceDue = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
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
            Quantity = 2,
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
    public async Task Handle_ValidCommand_SendsTwoEmails()
    {
        // Arrange
        var (user, company, invoice) = await SeedInvoiceScenarioAsync();
        SetCurrentUser(user.Id, user.Email);

        var emailService = Substitute.For<IEmailService>();
        var emailTemplateService = Substitute.For<IEmailTemplateService>();
        emailTemplateService
            .RenderTemplate(Arg.Any<EmailTemplateName>(), Arg.Any<Dictionary<string, string>>())
            .Returns("<html>template</html>");
        emailService
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        var handler = new SendInvoiceEmailHandler(DbContext, CurrentUserService, emailService, emailTemplateService);

        // Act
        await handler.Handle(new SendInvoiceEmailCommand(invoice.Id, company.Id), CancellationToken.None);

        // Assert — one email to client, one to owner
        await emailService
            .Received(2)
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());

        await emailService
            .Received(1)
            .SendEmailAsync(
                Arg.Is<string>(to => to == "client@test.com"),
                Arg.Any<string>(),
                Arg.Any<string>()
            );

        await emailService
            .Received(1)
            .SendEmailAsync(
                Arg.Is<string>(to => to == user.Email),
                Arg.Any<string>(),
                Arg.Any<string>()
            );
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new SendInvoiceEmailHandler(
            DbContext,
            CurrentUserService,
            Substitute.For<IEmailService>(),
            Substitute.For<IEmailTemplateService>()
        );

        // Act & Assert
        var act = () =>
            handler.Handle(new SendInvoiceEmailCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
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
        var handler = new SendInvoiceEmailHandler(
            DbContext,
            CurrentUserService,
            Substitute.For<IEmailService>(),
            Substitute.For<IEmailTemplateService>()
        );

        // Act & Assert
        var act = () =>
            handler.Handle(new SendInvoiceEmailCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentInvoice_ThrowsInvoiceNotFoundException()
    {
        // Arrange
        var (user, company, _) = await SeedInvoiceScenarioAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new SendInvoiceEmailHandler(
            DbContext,
            CurrentUserService,
            Substitute.For<IEmailService>(),
            Substitute.For<IEmailTemplateService>()
        );

        // Act & Assert
        var act = () =>
            handler.Handle(
                new SendInvoiceEmailCommand(Guid.NewGuid(), company.Id),
                CancellationToken.None
            );
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (_, company, invoice) = await SeedInvoiceScenarioAsync();
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
        var handler = new SendInvoiceEmailHandler(
            DbContext,
            CurrentUserService,
            Substitute.For<IEmailService>(),
            Substitute.For<IEmailTemplateService>()
        );

        // Act & Assert
        var act = () =>
            handler.Handle(new SendInvoiceEmailCommand(invoice.Id, company.Id), CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_InvoiceBelongsToOtherCompany_ThrowsInvoiceNotFoundException()
    {
        // Arrange — seed two companies; try to send an invoice that belongs to company2 using company1's ID
        var (user, company1, _) = await SeedInvoiceScenarioAsync();

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

        SetCurrentUser(user.Id, user.Email);
        var handler = new SendInvoiceEmailHandler(
            DbContext,
            CurrentUserService,
            Substitute.For<IEmailService>(),
            Substitute.For<IEmailTemplateService>()
        );

        // Act — try to send invoice2 using company1's ID → invoice not found for company1
        var act = () =>
            handler.Handle(new SendInvoiceEmailCommand(invoice2.Id, company1.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvoiceNotFoundException>();
    }
}
