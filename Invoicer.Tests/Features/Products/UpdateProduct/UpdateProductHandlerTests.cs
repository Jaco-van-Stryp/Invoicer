using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Products.UpdateProduct;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Products.UpdateProduct;

[Collection("Database")]
public class UpdateProductHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
{
    private async Task<(
        User User,
        Domain.Entities.Company Company,
        Product Product
    )> SeedUserWithCompanyAndProductAsync()
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
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Original Widget",
            Price = 10m,
            Description = "Original description",
            ImageUrl = "https://test.com/original.png",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Users.AddAsync(user);
        await DbContext.Companies.AddAsync(company);
        await DbContext.Products.AddAsync(product);
        await DbContext.SaveChangesAsync();
        return (user, company, product);
    }

    [Fact]
    public async Task Handle_AllFieldsProvided_UpdatesAllFields()
    {
        // Arrange
        var (user, company, product) = await SeedUserWithCompanyAndProductAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: company.Id,
            ProductId: product.Id,
            Name: "Updated Widget",
            Price: 49.99m,
            Description: "Updated description",
            ImageUrl: "https://test.com/updated.png"
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Products.FindAsync(product.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Updated Widget");
        saved.Price.Should().Be(49.99m);
        saved.Description.Should().Be("Updated description");
        saved.ImageUrl.Should().Be("https://test.com/updated.png");
    }

    [Fact]
    public async Task Handle_OnlyNameProvided_UpdatesOnlyName()
    {
        // Arrange
        var (user, company, product) = await SeedUserWithCompanyAndProductAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: company.Id,
            ProductId: product.Id,
            Name: "New Name Only",
            Price: null,
            Description: null,
            ImageUrl: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Products.FindAsync(product.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("New Name Only");
        saved.Price.Should().Be(10m);
        saved.Description.Should().Be("Original description");
        saved.ImageUrl.Should().Be("https://test.com/original.png");
    }

    [Fact]
    public async Task Handle_OnlyPriceProvided_UpdatesOnlyPrice()
    {
        // Arrange
        var (user, company, product) = await SeedUserWithCompanyAndProductAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: company.Id,
            ProductId: product.Id,
            Name: null,
            Price: 99.99m,
            Description: null,
            ImageUrl: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Products.FindAsync(product.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Original Widget");
        saved.Price.Should().Be(99.99m);
        saved.Description.Should().Be("Original description");
    }

    [Fact]
    public async Task Handle_NoFieldsProvided_NothingChanges()
    {
        // Arrange
        var (user, company, product) = await SeedUserWithCompanyAndProductAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: company.Id,
            ProductId: product.Id,
            Name: null,
            Price: null,
            Description: null,
            ImageUrl: null
        );

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var saved = await DbContext.Products.FindAsync(product.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Original Widget");
        saved.Price.Should().Be(10m);
        saved.Description.Should().Be("Original description");
        saved.ImageUrl.Should().Be("https://test.com/original.png");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Name: "Doesn't Matter",
            Price: null,
            Description: null,
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
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            Name: "No Company",
            Price: null,
            Description: null,
            ImageUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var (user, company, _) = await SeedUserWithCompanyAndProductAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: company.Id,
            ProductId: Guid.NewGuid(),
            Name: "No Product",
            Price: null,
            Description: null,
            ImageUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_CompanyBelongsToOtherUser_ThrowsCompanyNotFoundException()
    {
        // Arrange
        var (user1, company, product) = await SeedUserWithCompanyAndProductAsync();
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
        var handler = new UpdateProductHandler(DbContext, CurrentUserService);

        var command = new UpdateProductCommand(
            CompanyId: company.Id,
            ProductId: product.Id,
            Name: "Hijacked",
            Price: null,
            Description: null,
            ImageUrl: null
        );

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
