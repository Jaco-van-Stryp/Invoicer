using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Invoicer.Features.Products.CreateProduct;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Products.DeleteProduct;

[Collection("Database")]
public class DeleteProductEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
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
        var response = await client.PostAsJsonAsync("/company/create-company", payload);
        var result = await response.Content.ReadFromJsonAsync<CompanyResult>();
        return result!.Id;
    }

    private async Task<CreateProductResponse> CreateProductAsync(HttpClient client, Guid companyId)
    {
        var payload = new
        {
            CompanyId = companyId,
            Name = "Widget",
            Price = 29.99m,
            Description = "A widget",
            ImageUrl = "https://test.com/widget.png",
        };
        var response = await client.PostAsJsonAsync("/product/create-product", payload);
        return (await response.Content.ReadFromJsonAsync<CreateProductResponse>())!;
    }

    [Fact]
    public async Task DeleteProduct_Unauthenticated_Returns401()
    {
        // Act
        var response = await Client.DeleteAsync(
            $"/product/delete-product/{Guid.NewGuid()}/{Guid.NewGuid()}"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_Authenticated_Returns204AndRemovesProduct()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("deleter@test.com");
        var companyId = await CreateCompanyForUserAsync(client);
        var product = await CreateProductAsync(client, companyId);

        // Act
        var response = await client.DeleteAsync(
            $"/product/delete-product/{companyId}/{product.ProductId}"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify product is gone from database
        using var dbContext = CreateDbContext();
        var deleted = await dbContext.Products.FindAsync(product.ProductId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProduct_NonExistentProduct_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("user@test.com");
        var companyId = await CreateCompanyForUserAsync(client);

        // Act
        var response = await client.DeleteAsync(
            $"/product/delete-product/{companyId}/{Guid.NewGuid()}"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_CompanyBelongsToOtherUser_Returns404()
    {
        // Arrange — user1 creates company and product
        var (client1, _) = await CreateAuthenticatedUserAsync("user1@test.com");
        var companyId = await CreateCompanyForUserAsync(client1);
        var product = await CreateProductAsync(client1, companyId);

        // user2 tries to delete it
        var (client2, _) = await CreateAuthenticatedUserAsync("user2@test.com");

        // Act
        var response = await client2.DeleteAsync(
            $"/product/delete-product/{companyId}/{product.ProductId}"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record CompanyResult(Guid Id);
}
