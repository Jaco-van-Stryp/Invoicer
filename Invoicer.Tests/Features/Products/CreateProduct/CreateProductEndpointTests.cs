using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Invoicer.Features.Products.CreateProduct;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Products.CreateProduct;

[Collection("Database")]
public class CreateProductEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
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

    [Fact]
    public async Task CreateProduct_Authenticated_Returns200WithProduct()
    {
        // Arrange
        var (client, user) = await CreateAuthenticatedUserAsync("creator@test.com");
        var companyId = await CreateCompanyForUserAsync(client);

        var payload = new
        {
            CompanyId = companyId,
            Name = "Widget",
            Price = 29.99m,
            Description = "A useful widget",
            ImageUrl = "https://test.com/widget.png",
        };

        // Act
        var response = await client.PostAsJsonAsync("/product/create-product", payload);

        // Assert — HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        result.ProductId.Should().NotBeEmpty();
        result.Name.Should().Be("Widget");
        result.Price.Should().Be(29.99m);

        // Assert — verify in database
        using var dbContext = CreateDbContext();
        var saved = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == result.ProductId);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Widget");
        saved.CompanyId.Should().Be(companyId);
    }

    [Fact]
    public async Task CreateProduct_NonExistentCompany_ReturnsError()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("user@test.com");

        var payload = new
        {
            CompanyId = Guid.NewGuid(),
            Name = "Orphan Product",
            Price = 10m,
            Description = "No company",
            ImageUrl = "",
        };

        // Act
        var response = await client.PostAsJsonAsync("/product/create-product", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record CompanyResult(Guid Id);
}
