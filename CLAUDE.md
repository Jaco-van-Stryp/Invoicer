# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet build                                          # Build the project
dotnet run --project Invoicer                         # Run the API
dotnet ef migrations add <Name> --project Invoicer    # Add EF Core migration
dotnet ef database update --project Invoicer          # Apply migrations
```

dotnet test Invoicer.Tests                             # Run all tests (requires Docker)
dotnet test Invoicer.Tests --filter "ClassName"         # Run specific test class

## Architecture

This is a .NET 10 Web API using **Minimal APIs** with a feature-based folder structure and **CQRS via MediatR**.

### Project Layout

```
Invoicer/
├── Domain/
│   ├── Data/          # AppDbContext (PostgreSQL via Npgsql)
│   ├── Entities/      # Domain models (User, Company, Client, Invoice, Product, etc.)
│   └── Exceptions/    # Custom ApiException subclasses with HTTP status codes
├── Features/          # CQRS feature modules (one folder per domain aggregate)
│   ├── Auth/          # Passwordless email auth (JWT + 6-digit codes via AWS SES)
│   └── Company/       # Company management
├── Infrastructure/    # Cross-cutting services (DI-registered in Program.cs)
│   ├── CurrentUserService/     # Extracts UserId/Email from JWT claims
│   ├── EmailService/           # AWS SES v2 wrapper
│   ├── EmailTemplateService/   # Embedded HTML templates with placeholder replacement
│   ├── ExceptionHandling/      # GlobalExceptionHandler → ProblemDetails
│   ├── JWTTokenService/        # JWT generation
│   └── Validation/             # MediatR ValidationBehavior pipeline
└── Migrations/        # EF Core migrations
```

### Adding a New Feature

Each feature action follows this file convention inside `Features/{Aggregate}/{Action}/`:

| File | Purpose |
|------|---------|
| `{Action}Command.cs` or `{Action}Query.cs` | MediatR request (readonly record struct with validation attributes) |
| `{Action}Handler.cs` | `IRequestHandler<TRequest, TResponse>` implementation |
| `{Action}Endpoint.cs` | Static extension method mapping the Minimal API route |
| `{Action}Response.cs` | Response record |

Endpoints are registered in `Program.cs` via chained `.Map{Action}Endpoint()` calls on a route group (e.g., `app.MapGroup("company").MapCreateCompanyEndpoint()`).

### Validation

Request validation uses `System.ComponentModel.DataAnnotations` attributes on command/query records, processed automatically by `ValidationBehavior<,>` in the MediatR pipeline (before the handler runs).

### Authentication

Passwordless email-based flow:
1. `POST /auth/GetAccessToken` — sends a 6-digit code via SES, returns an `AccessTokenKey` (Guid)
2. `POST /auth/login` — validates code + key, returns a JWT (8-hour expiry)
3. Endpoints requiring auth use `.RequireAuthorization()`
4. `CurrentUserService` resolves the authenticated user's ID/email from JWT claims

### Key Conventions

- Commands/queries are `readonly record struct`
- Entities use `Guid` primary keys
- `User.RowVersion` uses PostgreSQL `xmin` for optimistic concurrency
- Custom exceptions extend `ApiException(message, statusCode)` and are caught by `GlobalExceptionHandler`
- Email templates are embedded resources (configured in `.csproj`, cached at runtime)

## Testing

Tests live in `Invoicer.Tests/` using **xUnit** + **FluentAssertions** + **NSubstitute** + **Testcontainers** (PostgreSQL) + **Respawn**.

- **Docker must be running** — Testcontainers spins up a real PostgreSQL container
- Tests mirror the feature folder structure: `Invoicer.Tests/Features/{Aggregate}/{Action}/`
- Two test base classes in `Invoicer.Tests/Infrastructure/`:
  - `IntegrationTestBase` — for handler-level tests (real DB, mocked ICurrentUserService)
  - `FunctionalTestBase` — for endpoint-level HTTP tests (WebApplicationFactory, fake auth)
- All test classes use `[Collection("Database")]` to share a single PostgreSQL container
- Respawn resets the database between each test (clean state, no test interference)

### Adding Tests for a New Feature

1. Create `Invoicer.Tests/Features/{Aggregate}/{Action}/{Action}HandlerTests.cs`
2. Extend `IntegrationTestBase`, use `[Collection("Database")]`
3. For endpoint tests, extend `FunctionalTestBase`

## .NET 10 / OpenApi v2.x Notes

- `Microsoft.OpenApi` v2.x removed the `Models` sub-namespace — all types live in `Microsoft.OpenApi` directly
- Use `OpenApiSecuritySchemeReference` instead of `OpenApiReference` for security schemes
- Do not mix `Microsoft.AspNetCore.OpenApi` with Swashbuckle — causes assembly conflicts
