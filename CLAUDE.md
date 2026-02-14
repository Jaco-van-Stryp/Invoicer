# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
dotnet build                                          # Build the project
dotnet run --project Invoicer                         # Run the API
dotnet ef migrations add <Name> --project Invoicer    # Add EF Core migration
dotnet ef database update --project Invoicer          # Apply migrations
dotnet test Invoicer.Tests                            # Run all tests (requires Docker)
dotnet test Invoicer.Tests --filter "ClassName"       # Run specific test class
```

## Architecture

.NET 10 Web API using **Minimal APIs** with a feature-based folder structure and **CQRS via MediatR**.

### Project Layout

```
Invoicer/
├── Domain/
│   ├── Data/          # AppDbContext (PostgreSQL via Npgsql)
│   ├── Entities/      # User, Company, Client, Invoice, Product, ProductInvoice, AuthToken
│   └── Exceptions/    # ApiException subclasses with HTTP status codes
├── Features/          # CQRS feature modules (one folder per domain aggregate)
│   ├── Auth/          # Passwordless email auth (JWT + 6-digit codes via AWS SES)
│   ├── Client/        # Client management
│   ├── Company/       # Company management
│   ├── File/          # File upload/download (MinIO)
│   ├── Invoice/       # Invoice management
│   └── Product/       # Product management
├── Infrastructure/
│   ├── CurrentUserService/     # Extracts UserId/Email from JWT claims
│   ├── DependencyInjection/    # Extension methods for service registration
│   ├── EmailService/           # AWS SES v2 wrapper
│   ├── EmailTemplateService/   # Embedded HTML templates with placeholder replacement
│   ├── ExceptionHandling/      # GlobalExceptionHandler → ProblemDetails
│   ├── JWTTokenService/        # JWT generation
│   ├── StorageService/         # MinIO (S3-compatible) file storage
│   └── Validation/             # MediatR ValidationBehavior pipeline
└── Migrations/
```

### Entity Relationships

- **User** (1) → (N) **Company** (the main aggregate root)
- **Company** (1) → (N) **Product**, **Client**, **Invoice**
- **Invoice** (N) → (1) **Client**, (N) → (1) **Company**
- **Invoice** (N) ↔ (N) **Product** via **ProductInvoice** join table (includes denormalized `CompanyId`)

### Endpoint Registration

`Program.cs` calls `app.MapEndpoints()` which creates a `/api` root group. Each feature has a `{Feature}Endpoints.cs` that creates a sub-group (e.g., `/api/client`) and chains individual endpoint mappings:

```
EndpointExtensions.MapEndpoints()  →  /api
  ├── ClientEndpoints              →  /api/client
  ├── CompanyEndpoints             →  /api/company
  ├── InvoiceEndpoints             →  /api/invoice
  ├── ProductEndpoints             →  /api/product
  ├── FileEndpoints                →  /api/file
  └── AuthEndpoints                →  /api/auth
```

Endpoints inject `ISender` (not `IMediator`) from MediatR. URL paths use kebab-case (`create-client`, `all-clients`).

### Adding a New Feature

Each feature action follows this file convention inside `Features/{Aggregate}/{Action}/`:

| File                                       | Purpose                                                               |
| ------------------------------------------ | --------------------------------------------------------------------- |
| `{Action}Command.cs` or `{Action}Query.cs` | MediatR request (`readonly record struct` with validation attributes) |
| `{Action}Handler.cs`                       | `IRequestHandler<TRequest, TResponse>` implementation                 |
| `{Action}Endpoint.cs`                      | Static extension method mapping the Minimal API route                 |
| `{Action}Response.cs`                      | Response record                                                       |

Also add the aggregate-level `{Feature}Endpoints.cs` if it doesn't exist, and wire it in `EndpointExtensions.MapEndpoints()`.

### Validation

Request validation uses `System.ComponentModel.DataAnnotations` attributes on command/query records, processed automatically by `ValidationBehavior<,>` in the MediatR pipeline (before the handler runs).

### Authentication

Passwordless email-based flow:

1. `POST /api/auth/get-access-token` — sends a 6-digit code via SES, returns an `AccessTokenKey` (Guid)
2. `POST /api/auth/login` — validates code + key, returns a JWT (8-hour expiry)
3. Endpoints requiring auth use `.RequireAuthorization()`
4. `CurrentUserService` resolves the authenticated user's ID/email from JWT claims
5. Handlers authorize ownership by checking `user.Companies.FirstOrDefault(c => c.Id == request.CompanyId)`

### Key Conventions

- Commands/queries are `readonly record struct`
- Entities use `Guid` primary keys with `init` accessors
- Only `User.RowVersion` uses PostgreSQL `xmin` for optimistic concurrency
- Custom exceptions extend `ApiException(message, statusCode)` and are caught by `GlobalExceptionHandler`
- Email templates are embedded resources (configured in `.csproj`, cached at runtime)
- File storage uses MinIO (S3-compatible) — files stored with Guid filenames via `IStorageService`

### Required Configuration Sections (appsettings.json)

- `ConnectionStrings:DefaultConnection` — PostgreSQL
- `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`
- `SES:Region`, `SES:FromEmail`, `SES:FromName`
- `MinIO:Endpoint`, `MinIO:AccessKey`, `MinIO:SecretKey`, `MinIO:BucketName`

## Testing

Tests live in `Invoicer.Tests/` using **xUnit** + **FluentAssertions** + **NSubstitute** + **Testcontainers** (PostgreSQL) + **Respawn**.

- **Docker must be running** — Testcontainers spins up a real PostgreSQL container
- Tests mirror the feature folder structure: `Invoicer.Tests/Features/{Aggregate}/{Action}/`
- All test classes **must** use `[Collection("Database")]` to share a single PostgreSQL container
- Respawn resets the database between each test (clean state, no test interference)
- The main project uses `<InternalsVisibleTo>Invoicer.Tests</InternalsVisibleTo>`

### Test Strategy

**All tests are handler-level integration tests** — instantiate the handler directly with a real `AppDbContext` and mocked `ICurrentUserService`. Do **not** write endpoint/HTTP-level tests (no `WebApplicationFactory`, no `HttpClient`). The whole point of MediatR/CQRS is that business logic lives in handlers, so test handlers directly.

### Test Base Class (`Invoicer.Tests/Infrastructure/`)

**`IntegrationTestBase`** — the only test base class:

- Provides `AppDbContext DbContext` and mocked `ICurrentUserService`
- Helper: `SetCurrentUser(Guid userId, string email)` to configure the mock
- Helper: `CreateDbContext()` — creates a fresh `AppDbContext` for assertion queries
- Call `DbContext.ChangeTracker.Clear()` before assertions to force DB round-trips

### Adding Tests for a New Feature

1. Create `Invoicer.Tests/Features/{Aggregate}/{Action}/{Action}HandlerTests.cs`
2. Extend `IntegrationTestBase`, use `[Collection("Database")]`
3. Instantiate the handler directly: `new XxxHandler(DbContext, CurrentUserService)`
4. Test happy paths, authorization (wrong user), and error cases (not found) via handler exceptions
5. xUnit 2.9.x `IAsyncLifetime` uses `Task` return types, **not** `ValueTask`

## Gotchas

- **Namespace collision**: `Invoicer.Domain.Entities.Company` collides with `Invoicer.Features.Company` namespace — use `Domain.Entities.Company` to disambiguate in feature files
- **ValidationBehavior boxing**: `readonly record struct` causes validation mismatch — box once with `object instance = request;` then pass the same boxed instance to both `ValidationContext` and `TryValidateObject`
- **OpenApi v2.x** (`Microsoft.OpenApi` v2.4.1): removed the `Models` sub-namespace — all types live in `Microsoft.OpenApi` directly. Use `OpenApiSecuritySchemeReference` instead of `OpenApiReference`. Do not mix `Microsoft.AspNetCore.OpenApi` with Swashbuckle (assembly conflicts)
