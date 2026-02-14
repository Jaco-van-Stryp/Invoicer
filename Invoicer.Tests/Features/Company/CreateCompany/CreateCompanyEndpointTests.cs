using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Invoicer.Features.Company.CreateCompany;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Company.CreateCompany;

[Collection("Database")]
public class CreateCompanyEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
{
    [Fact]
    public async Task CreateCompany_Unauthenticated_Returns401()
    {
        // Arrange — use the default unauthenticated client
        var payload = new { Name = "Should Not Work" };

        // Act — send the request without any auth headers
        var response = await Client.PostAsJsonAsync("/api/company/create-company", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCompany_Authenticated_Returns200WithCompanyId()
    {
        // Arrange — create a user in DB and get an authenticated HTTP client
        var (client, user) = await CreateAuthenticatedUserAsync("creator@test.com");

        var payload = new
        {
            Name = "Endpoint Test Corp",
            Address = "456 Test Avenue",
            TaxNumber = "TAX-999",
            PhoneNumber = "555-0200",
            Email = "contact@endpoint-test.com",
            PaymentDetails = "Crypto: 0xABC",
            LogoUrl = "https://test.com/logo.png",
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/company/create-company", payload);

        // Assert — HTTP response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateCompanyResponse>();
        result.Id.Should().NotBeEmpty();

        // Assert — verify in database
        using var dbContext = CreateDbContext();
        var saved = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == result.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Endpoint Test Corp");
        saved.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateCompany_AuthenticatedButUserDeletedFromDb_ReturnsError()
    {
        // Arrange — create an authenticated client with a user ID that doesn't exist in DB
        var ghostUserId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(ghostUserId, "ghost@test.com");

        var payload = new { Name = "Ghost Company" };

        // Act
        var response = await client.PostAsJsonAsync("/api/company/create-company", payload);

        // Assert — should return 401 (UserNotFoundException maps to 401)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
