using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using global::Invoicer.Features.Products.CreateProduct;
using global::Invoicer.Features.Products.GetAllProducts;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Products.GetAllProducts;

[Collection("Database")]
public class GetAllProductsEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
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

    private async Task CreateProductAsync(HttpClient client, Guid companyId, string name, decimal price)
    {
        var payload = new
        {
            CompanyId = companyId,
            Name = name,
            Price = price,
            Description = $"Description for {name}",
            ImageUrl = $"https://test.com/{name.ToLower()}.png",
        };
        await client.PostAsJsonAsync("/product/create-product", payload);
    }

    [Fact]
    public async Task GetAllProducts_Unauthenticated_Returns401()
    {
        // Act
        var response = await Client.GetAsync($"/product/get-all-products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllProducts_Authenticated_ReturnsProducts()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("user@test.com");
        var companyId = await CreateCompanyForUserAsync(client);
        await CreateProductAsync(client, companyId, "Widget", 10m);
        await CreateProductAsync(client, companyId, "Gadget", 20m);

        // Act
        var response = await client.GetAsync($"/product/get-all-products/{companyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllProductsResponse>>();
        result.Should().HaveCount(2);
        result!.Select(p => p.Name).Should().Contain(["Widget", "Gadget"]);
    }

    [Fact]
    public async Task GetAllProducts_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("user@test.com");
        var companyId = await CreateCompanyForUserAsync(client);

        // Act
        var response = await client.GetAsync($"/product/get-all-products/{companyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<GetAllProductsResponse>>();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllProducts_NonExistentCompany_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("user@test.com");

        // Act
        var response = await client.GetAsync($"/product/get-all-products/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record CompanyResult(Guid Id);
}
