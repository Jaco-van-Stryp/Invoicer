using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Invoicer.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Tests.Features.Company.UpdateCompanyDetails;

[Collection("Database")]
public class UpdateCompanyDetailsEndpointTests(DatabaseFixture db) : FunctionalTestBase(db)
{
    [Fact]
    public async Task UpdateCompanyDetails_Unauthenticated_Returns401()
    {
        // Act
        var payload = new { CompanyId = Guid.NewGuid(), Name = "Nope" };
        var response = await Client.PatchAsJsonAsync("/api/company/update-company-details", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCompanyDetails_Authenticated_UpdatesCompany()
    {
        // Arrange — create user and company
        var (client, user) = await CreateAuthenticatedUserAsync("updater@test.com");

        var createPayload = new
        {
            Name = "Before Update",
            Address = "Old Address",
            TaxNumber = "TAX-OLD",
            PhoneNumber = "555-OLD",
            Email = "old@test.com",
            PaymentDetails = "Bank: Old",
            LogoUrl = "https://old.com/logo.png",
        };
        var createResponse = await client.PostAsJsonAsync("/api/company/create-company", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCompanyResult>();

        // Act — patch with updated fields
        var updatePayload = new
        {
            CompanyId = created!.Id,
            Name = "After Update",
            Address = "New Address",
        };
        var response = await client.PatchAsJsonAsync("/api/company/update-company-details", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var dbContext = CreateDbContext();
        var saved = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == created.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("After Update");
        saved.Address.Should().Be("New Address");
        // Unchanged fields should remain
        saved.TaxNumber.Should().Be("TAX-OLD");
        saved.PhoneNumber.Should().Be("555-OLD");
    }

    [Fact]
    public async Task UpdateCompanyDetails_CompanyNotFound_Returns404()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedUserAsync("notfound@test.com");

        var payload = new
        {
            CompanyId = Guid.NewGuid(),
            Name = "Ghost Company",
        };

        // Act
        var response = await client.PatchAsJsonAsync("/api/company/update-company-details", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCompanyDetails_CompanyBelongsToOtherUser_Returns404()
    {
        // Arrange — user1 creates a company
        var (client1, user1) = await CreateAuthenticatedUserAsync("user1@test.com");
        var createPayload = new { Name = "User1 Corp" };
        var createResponse = await client1.PostAsJsonAsync("/api/company/create-company", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateCompanyResult>();

        // Arrange — user2 tries to update it
        var (client2, _) = await CreateAuthenticatedUserAsync("user2@test.com");

        var updatePayload = new
        {
            CompanyId = created!.Id,
            Name = "Hijacked",
        };

        // Act
        var response = await client2.PatchAsJsonAsync("/api/company/update-company-details", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private record CreateCompanyResult(Guid Id);
}
