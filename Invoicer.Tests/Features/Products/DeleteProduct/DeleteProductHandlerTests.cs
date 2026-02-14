using FluentAssertions;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Products.DeleteProduct;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Products.DeleteProduct;

[Collection("Database")]
public class DeleteProductHandlerTests(DatabaseFixture db) : IntegrationTestBase(db)
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
            Name = "Doomed Widget",
            Price = 10m,
            Description = "About to be deleted",
            ImageUrl = "https://test.com/doomed.png",
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
    public async Task Handle_ValidCommand_DeletesProductFromDatabase()
    {
        // Arrange
        var (user, company, product) = await SeedUserWithCompanyAndProductAsync();
        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteProductHandler(DbContext, CurrentUserService);

        var command = new DeleteProductCommand(CompanyId: company.Id, ProductId: product.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Products.FindAsync(product.Id);
        deleted.Should().BeNull("the product should have been removed from the database");
    }

    [Fact]
    public async Task Handle_DeleteOneProduct_OtherProductsRemain()
    {
        // Arrange
        var (user, company, product1) = await SeedUserWithCompanyAndProductAsync();
        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Survivor Widget",
            Price = 20m,
            Description = "Should remain",
            ImageUrl = "https://test.com/survivor.png",
            CompanyId = company.Id,
            Company = company,
        };
        await DbContext.Products.AddAsync(product2);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, user.Email);
        var handler = new DeleteProductHandler(DbContext, CurrentUserService);

        // Act — delete only the first product
        await handler.Handle(
            new DeleteProductCommand(company.Id, product1.Id),
            CancellationToken.None
        );

        // Assert
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Products.FindAsync(product1.Id);
        deleted.Should().BeNull();

        var remaining = await DbContext.Products.FindAsync(product2.Id);
        remaining.Should().NotBeNull();
        remaining!.Name.Should().Be("Survivor Widget");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsUserNotFoundException()
    {
        // Arrange
        SetCurrentUser(Guid.NewGuid());
        var handler = new DeleteProductHandler(DbContext, CurrentUserService);

        var command = new DeleteProductCommand(
            CompanyId: Guid.NewGuid(),
            ProductId: Guid.NewGuid()
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
        var handler = new DeleteProductHandler(DbContext, CurrentUserService);

        var command = new DeleteProductCommand(
            CompanyId: Guid.NewGuid(),
            ProductId: Guid.NewGuid()
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
        var handler = new DeleteProductHandler(DbContext, CurrentUserService);

        var command = new DeleteProductCommand(CompanyId: company.Id, ProductId: Guid.NewGuid());

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
        var handler = new DeleteProductHandler(DbContext, CurrentUserService);

        var command = new DeleteProductCommand(CompanyId: company.Id, ProductId: product.Id);

        // Act & Assert
        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<CompanyNotFoundException>();
    }
}
