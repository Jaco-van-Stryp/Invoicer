using System.Security.Claims;
using System.Text.Encodings.Web;
using Amazon.SimpleEmailV2;
using Invoicer.Domain.Data;
using Invoicer.Infrastructure.EmailService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Invoicer.Tests.Infrastructure;

public class InvoicerApiFactory : WebApplicationFactory<Program>
{
    private readonly DatabaseFixture _db;

    public InvoicerApiFactory(DatabaseFixture db)
    {
        _db = db;
        // Set JWT config as environment variables so Program.cs can read them
        // during WebApplication.CreateBuilder() (before ConfigureAppConfiguration runs)
        Environment.SetEnvironmentVariable("Jwt__Secret", "ThisIsAFakeTestSecretKeyThatIsLongEnough123!");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the real DbContext registration
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // Register DbContext pointing at the test container
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(_db.ConnectionString));

            // Replace AWS SES with a mock (we don't send real emails in tests)
            var sesDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IAmazonSimpleEmailServiceV2)
            );
            if (sesDescriptor != null)
                services.Remove(sesDescriptor);
            services.AddSingleton(Substitute.For<IAmazonSimpleEmailServiceV2>());

            var emailDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IEmailService)
            );
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);
            services.AddScoped(_ => Substitute.For<IEmailService>());

            // Add fake authentication scheme for testing
            services
                .AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
        });

        builder.UseEnvironment("Development");
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, string email = "test@test.com")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Email", email);
        return client;
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder
    )
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If no test header, fail authentication (simulates unauthenticated request)
        if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader))
            return Task.FromResult(AuthenticateResult.Fail("No test user header"));

        var userId = userIdHeader.ToString();
        var email = Request.Headers.TryGetValue("X-Test-Email", out var emailHeader)
            ? emailHeader.ToString()
            : "test@test.com";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
