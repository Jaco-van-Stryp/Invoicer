using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Invoicer.Features.Products.CreateProduct;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Products.UpdateProduct;

[Collection("Database")]
public class UpdateProductEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
{
    private async Task<Guid> CreateCompanyForUserAsync(HttpClient client)
    {
        var payload = new
        {
            Name = "Test Corp",
            Address = "123 Test St",
            TaxNumber = "TAX-123",
            PhoneNumber = "555-0100",
            Email = "test@corp.com",
            PaymentDetails = "Bank: Test",
            LogoUrl = "https://test.com/logo.png",
        };
        var response = await client.PostAsJsonAsync("/api/company/create-company", payload);
        var result = await response.Content.ReadFromJsonAsync<CompanyResult>();
        return result!.Id;
    }

    private async Task<CreateProductResponse> CreateProductAsync(
        HttpClient client,
        Guid companyId,
        string name = "Widget",
        decimal price = 29.99m
    )
    {
        var payload = new
        {
            CompanyId = companyId,
            Name = name,
            Price = price,
            Description = $"Description for {name}",
            ImageUrl = $"https://test.com/{name.ToLower()}.png",
        };
        var response = await client.PostAsJsonAsync("/api/product/create-product", payload);
        return (await response.Content.ReadFromJsonAsync<CreateProductResponse>())!;
    }

    [Fact]
    public async Task UpdateProduct_Unauthenticated_Returns401()
    {
        // Act
        var payload = new
        {
            CompanyId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            Name = "Nope",
        };
        var response = await Client.PatchAsJsonAsync("/api/product/update-product", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_Authenticated_UpdatesProduct()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("updater@test.com");
        var companyId = await CreateCompanyForUserAsync(client);
        var product = await CreateProductAsync(client, companyId);

        // Act
        var updatePayload = new
        {
            CompanyId = companyId,
            ProductId = product.ProductId,
            Name = "Updated Widget",
            Price = 49.99m,
        };
        var response = await client.PatchAsJsonAsync("/api/product/update-product", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var dbContext = CreateDbContext();
        var saved = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == product.ProductId);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Updated Widget");
        saved.Price.Should().Be(49.99m);
        // Unchanged field
        saved.Description.Should().Be("Description for Widget");
    }

    [Fact]
    public async Task UpdateProduct_NonExistentProduct_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("user@test.com");
        var companyId = await CreateCompanyForUserAsync(client);

        var payload = new
        {
            CompanyId = companyId,
            ProductId = Guid.NewGuid(),
            Name = "Ghost Product",
        };

        // Act
        var response = await client.PatchAsJsonAsync("/api/product/update-product", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProduct_CompanyBelongsToOtherUser_Returns404()
    {
        // Arrange — user1 creates a company with a product
        var (client1, _) = await CreateAuthenticatedUserAsync("user1@test.com");
        var companyId = await CreateCompanyForUserAsync(client1);
        var product = await CreateProductAsync(client1, companyId);

        // user2 tries to update the product
        var (client2, _) = await CreateAuthenticatedUserAsync("user2@test.com");

        var payload = new
        {
            CompanyId = companyId,
            ProductId = product.ProductId,
            Name = "Hijacked",
        };

        // Act
        var response = await client2.PatchAsJsonAsync("/api/product/update-product", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record CompanyResult(Guid Id);
}
