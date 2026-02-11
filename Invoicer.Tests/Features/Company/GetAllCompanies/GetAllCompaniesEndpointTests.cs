using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Invoicer.Features.Company.GetAllCompanies;
using Invoicer.Tests.Infrastructure;

namespace Invoicer.Tests.Features.Company.GetAllCompanies;

[Collection("Database")]
public class GetAllCompaniesEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
{
    [Fact]
    public async Task GetAllCompanies_Unauthenticated_Returns401()
    {
        // Act
        var response = await Client.GetAsync("/company/get-all-companies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllCompanies_Authenticated_ReturnsCompanies()
    {
        // Arrange — create user and seed a company via the create endpoint
        var (client, user) = await CreateAuthenticatedUserAsync("list@test.com");

        var createPayload = new
        {
            Name = "Listed Corp",
            Address = "100 List Ave",
            TaxNumber = "TAX-LIST",
            PhoneNumber = "555-LIST",
            Email = "listed@test.com",
            PaymentDetails = "Bank: List",
            LogoUrl = "https://list.com/logo.png",
        };
        await client.PostAsJsonAsync("/company/CreateCompany", createPayload);

        // Act
        var response = await client.GetAsync("/company/get-all-companies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetAllCompaniesResponse>>();
        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result![0].Name.Should().Be("Listed Corp");
    }

    [Fact]
    public async Task GetAllCompanies_NoCompanies_ReturnsEmptyList()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("nocompanies@test.com");

        // Act
        var response = await client.GetAsync("/company/get-all-companies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<GetAllCompaniesResponse>>();
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
