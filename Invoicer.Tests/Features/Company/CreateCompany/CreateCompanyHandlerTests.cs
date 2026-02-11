using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Company.CreateCompany;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Company.CreateCompany;

[Collection("Database")]
public class CreateCompanyHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesCompanyInDatabase()
    {
        // Arrange — seed a user in the database
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
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        // Tell the mocked ICurrentUserService to return this user's ID
        SetCurrentUser(user.Id, user.Email);

        // Create the handler (real DbContext + mocked CurrentUserService)
        var handler = new CreateCompanyHandler(DbContext, CurrentUserService);

        var command = new CreateCompanyCommand(
            Name: "Acme Corp",
            Address: "123 Main Street",
            TaxNumber: "TAX-12345",
            PhoneNumber: "555-0100",
            Email: "billing@acme.com",
            PaymentDetails: "Bank: FNB, Acc: 12345",
            LogoUrl: "https://acme.com/logo.png"
        );

        // Act — execute the handler
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — verify the response
        result.Id.Should().NotBeEmpty("a new company should get a generated GUID");

        // Assert — verify the data was actually persisted in the database
        var saved = await DbContext.Companies.FindAsync(result.Id);
        saved.Should().NotBeNull("the company should exist in the database");
        saved!.Name.Should().Be("Acme Corp");
        saved.Address.Should().Be("123 Main Street");
        saved.TaxNumber.Should().Be("TAX-12345");
        saved.PhoneNumber.Should().Be("555-0100");
        saved.Email.Should().Be("billing@acme.com");
        saved.PaymentDetails.Should().Be("Bank: FNB, Acc: 12345");
        saved.LogoUrl.Should().Be("https://acme.com/logo.png");
        saved.UserId.Should().Be(user.Id, "the company should be linked to the creating user");
    }

    [Fact]
    public async Task Handle_NullOptionalFields_DefaultsToEmptyStrings()
    {
        // Arrange
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
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id);
        var handler = new CreateCompanyHandler(DbContext, CurrentUserService);

        // Only Name is required; everything else is null
        var command = new CreateCompanyCommand(
            Name: "Minimal Co",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — null fields should be stored as empty strings
        var saved = await DbContext.Companies.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Minimal Co");
        saved.Address.Should().BeEmpty();
        saved.TaxNumber.Should().BeEmpty();
        saved.PhoneNumber.Should().BeEmpty();
        saved.Email.Should().BeEmpty();
        saved.PaymentDetails.Should().BeEmpty();
        saved.LogoUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange — set a user ID that doesn't exist in the DB
        var fakeUserId = Guid.NewGuid();
        SetCurrentUser(fakeUserId);

        var handler = new CreateCompanyHandler(DbContext, CurrentUserService);
        var command = new CreateCompanyCommand(
            Name: "Ghost Corp",
            Address: null,
            TaxNumber: null,
            PhoneNumber: null,
            Email: null,
            PaymentDetails: null,
            LogoUrl: null
        );

        // Act & Assert — should throw UserNotFoundException
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task Handle_SameUserCreatesMultipleCompanies_AllPersisted()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "multi@test.com",
            AuthTokens = [],
            Companies = [],
            LoginAttempts = 0,
            IsLocked = false,
            LockoutEnd = null,
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id);
        var handler = new CreateCompanyHandler(DbContext, CurrentUserService);

        // Act — create two companies
        var result1 = await handler.Handle(
            new CreateCompanyCommand("Company A", null, null, null, null, null, null),
            CancellationToken.None
        );
        var result2 = await handler.Handle(
            new CreateCompanyCommand("Company B", null, null, null, null, null, null),
            CancellationToken.None
        );

        // Assert — both should exist with different IDs
        result1.Id.Should().NotBe(result2.Id);

        var companies = await DbContext.Companies.Where(c => c.UserId == user.Id).ToListAsync();
        companies.Should().HaveCount(2);
        companies.Select(c => c.Name).Should().Contain(["Company A", "Company B"]);
    }
}
