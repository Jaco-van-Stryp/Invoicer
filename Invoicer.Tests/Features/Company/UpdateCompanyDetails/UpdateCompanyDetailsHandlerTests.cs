using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Company.UpdateCompanyDetails;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Company.UpdateCompanyDetails;

[Collection("Database")]
public class UpdateCompanyDetailsHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(User User, Domain.Entities.Company Company)> SeedUserWithCompanyAsync()
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
            Name = "Original Corp",
            Address = "123 Original St",
            TaxNumber = "TAX-ORIG",
            PhoneNumber = "555-ORIG",
            Email = "original@test.com",
            PaymentDetails = "Bank: Original",
            LogoUrl = "https://original.com/logo.png",
            UserId = user.Id,
            User = user,
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.SaveChangesAsync();
        return (user, company);
    }

    [Fact]
    public async Task Handle_AllFieldsProvided_UpdatesAllFields()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateCompanyDetailsHandler(DbContext, CurrentUserService);

        var command = new UpdateCompanyDetailsCommand(
            CompanyId: company.Id,
            Name: "Updated Corp",
            Address: "456 Updated Ave",
            TaxNumber: "TAX-UPD",
            PhoneNumber: "555-UPD",
            Email: "updated@test.com",
            PaymentDetails: "Bank: Updated",
            LogoUrl: "https://updated.com/logo.png"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — use fresh context to verify database state
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Companies.FindAsync(company.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Updated Corp");
        saved.Address.Should().Be("456 Updated Ave");
        saved.TaxNumber.Should().Be("TAX-UPD");
        saved.PhoneNumber.Should().Be("555-UPD");
        saved.Email.Should().Be("updated@test.com");
        saved.PaymentDetails.Should().Be("Bank: Updated");
        saved.LogoUrl.Should().Be("https://updated.com/logo.png");
    }

    [Fact]
    public async Task Handle_OnlyNameProvided_UpdatesOnlyName()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateCompanyDetailsHandler(DbContext, CurrentUserService);

        var command = new UpdateCompanyDetailsCommand(
            CompanyId: company.Id,
            Name: "New Name Only",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — only name should change, everything else stays original
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Companies.FindAsync(company.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Name Only");
        saved.Address.Should().Be("123 Original St");
        saved.TaxNumber.Should().Be("TAX-ORIG");
        saved.PhoneNumber.Should().Be("555-ORIG");
        saved.Email.Should().Be("original@test.com");
        saved.PaymentDetails.Should().Be("Bank: Original");
        saved.LogoUrl.Should().Be("https://original.com/logo.png");
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_NothingChanges()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateCompanyDetailsHandler(DbContext, CurrentUserService);

        var command = new UpdateCompanyDetailsCommand(
            CompanyId: company.Id,
            Name: null,
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — everything stays original
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Companies.FindAsync(company.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Original Corp");
        saved.Address.Should().Be("123 Original St");
        saved.TaxNumber.Should().Be("TAX-ORIG");
        saved.PhoneNumber.Should().Be("555-ORIG");
        saved.Email.Should().Be("original@test.com");
        saved.PaymentDetails.Should().Be("Bank: Original");
        saved.LogoUrl.Should().Be("https://original.com/logo.png");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new UpdateCompanyDetailsHandler(DbContext, CurrentUserService);

        var command = new UpdateCompanyDetailsCommand(
            CompanyId: Guid.NewGuid(),
            Name: "Doesn't Matter",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange — create two users, company belongs to user1
        var (user1, company) = await SeedUserWithCompanyAsync();
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

        // Authenticate as user2
        SetCurrentUser(user2.Id, user2.Email);
        var handler = new UpdateCompanyDetailsHandler(DbContext, CurrentUserService);

        var command = new UpdateCompanyDetailsCommand(
            CompanyId: company.Id,
            Name: "Hijacked",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentCompanyId_ThrowsCompanyNotFoundException()
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
        var handler = new UpdateCompanyDetailsHandler(DbContext, CurrentUserService);

        var command = new UpdateCompanyDetailsCommand(
            CompanyId: Guid.NewGuid(),
            Name: "No Such Company",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
