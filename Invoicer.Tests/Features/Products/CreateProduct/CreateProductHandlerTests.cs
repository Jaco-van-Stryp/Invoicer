using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Products.CreateProduct;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Products.CreateProduct;

[Collection("Database")]
public class CreateProductHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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
        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.SaveChangesAsync();
        return (user, company);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProductInDatabase()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateProductHandler(DbContext, CurrentUserService);

        var command = new CreateProductCommand(
            CompanyId: company.Id,
            Name: "Widget",
            Price: 29.99m,
            Description: "A useful widget",
            ImageUrl: "https://test.com/widget.png"
        );

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — verify response
        result.ProductId.Should().NotBeEmpty();
        result.Name.Should().Be("Widget");
        result.Price.Should().Be(29.99m);
        result.Description.Should().Be("A useful widget");
        result.ImageUrl.Should().Be("https://test.com/widget.png");
        result.CompanyId.Should().Be(company.Id);

        // Assert — verify persistence (clear tracker to force DB round-trip)
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Products.FindAsync(result.ProductId);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Widget");
        saved.Price.Should().Be(29.99m);
        saved.Description.Should().Be("A useful widget");
        saved.ImageUrl.Should().Be("https://test.com/widget.png");
        saved.CompanyId.Should().Be(company.Id);
    }

    [Fact]
    public async Task Handle_MultipleProducts_AllPersisted()
    {
        // Arrange
        var (user, company) = await SeedUserWithCompanyAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new CreateProductHandler(DbContext, CurrentUserService);

        // Act
        var result1 = await handler.Handle(
            new CreateProductCommand(company.Id, "Product A", 10m, "Desc A", null),
            CancellationToken.None
        );
        var result2 = await handler.Handle(
            new CreateProductCommand(company.Id, "Product B", 20m, "Desc B", null),
            CancellationToken.None
        );

        // Assert
        result1.ProductId.Should().NotBe(result2.ProductId);

        DbContext.ChangeTracker.Clear();
        var products = await DbContext.Products.Where(p => p.CompanyId == company.Id).ToListAsync();
        products.Should().HaveCount(2);
        products.Select(p => p.Name).Should().Contain(["Product A", "Product B"]);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new CreateProductHandler(DbContext, CurrentUserService);

        var command = new CreateProductCommand(
            CompanyId: Guid.NewGuid(),
            Name: "Ghost Product",
            Price: 10m,
            Description: "Should fail",
            ImageUrl: null
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
        var handler = new CreateProductHandler(DbContext, CurrentUserService);

        var command = new CreateProductCommand(
            CompanyId: Guid.NewGuid(),
            Name: "Orphan Product",
            Price: 10m,
            Description: "No company",
            ImageUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange — company belongs to user1
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
        var handler = new CreateProductHandler(DbContext, CurrentUserService);

        var command = new CreateProductCommand(
            CompanyId: company.Id,
            Name: "Hijacked Product",
            Price: 10m,
            Description: "Not my company",
            ImageUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
