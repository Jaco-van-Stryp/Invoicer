# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context7 Library References

When working on this project, use Context7 (`mcp__context7__query-docs`) to look up current documentation. Resolve library IDs first with `mcp__context7__resolve-library-id`, or use these known IDs directly:

| Library        | Context7 ID               | Use for                                      |
| -------------- | ------------------------- | -------------------------------------------- |
| Angular 21     | `/websites/angular_dev`   | Components, signals, routing, SSR, DI        |
| PrimeNG 21     | `/websites/v20_primeng`   | UI components (Button, InputOtp, Table etc.) |
| Tailwind CSS   | _(resolve at query time)_ | Utility classes, responsive design           |
| .NET / EF Core | _(resolve at query time)_ | API, DbContext, migrations                   |

## Build & Run Commands

### Backend (.NET API)

```bash
dotnet build                                          # Build the project
dotnet run --project Invoicer                         # Run the API (https://localhost:7261)
dotnet ef migrations add <Name> --project Invoicer    # Add EF Core migration
dotnet ef database update --project Invoicer          # Apply migrations
dotnet test Invoicer.Tests                            # Run all tests (requires Docker)
dotnet test Invoicer.Tests --filter "ClassName"       # Run specific test class
```

### Frontend (Angular Client)

```bash
cd InvoicerClient
npm install                                           # Install dependencies
npm start                                             # ng serve (dev server on http://localhost:4200)
npm run build                                         # Production build with SSR
npm test                                              # Run unit tests (Vitest)
npm run generate                                      # Regenerate API client from Swagger spec
                                                      # (requires .NET API running on localhost:5244)
npm run serve:ssr:InvoicerClient                      # Serve SSR production build
```

## Architecture

Full-stack application: **.NET 10 Web API** backend + **Angular 21** frontend (SSR-enabled).

### Backend

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

### Frontend

**Angular 21** standalone-component app with SSR, PrimeNG UI, and Tailwind CSS.

#### Frontend Project Layout

```
InvoicerClient/
├── src/
│   ├── main.ts                     # Browser bootstrap
│   ├── main.server.ts              # SSR bootstrap
│   ├── server.ts                   # Express SSR server
│   ├── styles.css                  # Global styles (@import "tailwindcss")
│   └── app/
│       ├── app.ts                  # Root component (standalone, RouterOutlet)
│       ├── app.routes.ts           # Route definitions
│       ├── app.config.ts           # Providers (Router, PrimeNG Aura theme, hydration)
│       ├── Components/             # Feature components
│       │   └── Auth/
│       │       ├── login/          # Login page (embeds OTP)
│       │       ├── otp/            # 6-digit OTP input (PrimeNG InputOtp)
│       │       └── register/       # Stub
│       └── api/                    # AUTO-GENERATED — do not edit manually
│           ├── api/                # Service classes (Auth, Client, Company, Invoice, Product, File)
│           └── model/              # TypeScript interfaces for API request/response types
├── angular.json                    # CLI config (SSR enabled, @angular/build builder)
├── openapitools.json               # OpenAPI Generator config
├── tsconfig.json                   # Strict TypeScript
└── .postcssrc.json                 # Tailwind via @tailwindcss/postcss
```

#### Key Frontend Stack

- **Angular 21** — standalone components, signals, new control flow (`@if`, `@for`)
- **PrimeNG 21** — UI components (Aura theme via `@primeuix/themes`)
- **Tailwind CSS 4** — utility-first styling via PostCSS
- **Vitest** — unit testing (not Jasmine/Karma)
- **SSR** — Angular SSR with Express, hydration with event replay
- **TypeScript 5.9** — strict mode enabled

#### API Client Code Generation

The `InvoicerClient/src/app/api/` directory is **auto-generated** by OpenAPI Generator from the .NET backend's Swagger spec. **Do not edit files in this directory manually** — they will be overwritten.

To regenerate after backend API changes:

1. Run the .NET API (`dotnet run --project Invoicer`)
2. Run `npm run generate` from `InvoicerClient/`
3. Generated services are `@Injectable({ providedIn: 'root' })` and use `HttpClient`

Available generated services: `AuthService`, `ClientService`, `CompanyService`, `InvoiceService`, `ProductService`, `FileService`

#### Shared Styles (`InvoicerClient/src/styles/shared.css`)

Global reusable CSS imported via `styles.css`. All design tokens and common patterns live here — **never redeclare them in component CSS**.

| Category | Classes / Tokens | Used by |
|---|---|---|
| **Design tokens** | `--purple`, `--pink`, `--blue`, `--green`, `--indigo`, `--gradient`, `--glass-bg`, `--glass-border`, `--sidebar-width`, `--sidebar-rail-width` | All components (`:root`) |
| **Gradient text** | `.gradient-text` | Branding, headings |
| **Glass morphism** | `.glass-card` | Auth cards, modals |
| **Blob backgrounds** | `.blob`, `.blob-1`, `.blob-2`, `.blob-3` | Auth pages, landing |
| **Blob animations** | `blob-float-1/2/3`, `blob-move-1/2`, `pop-in` | Landing, auth, dashboard |
| **Auth layout** | `.auth-container`, `.auth-content`, `.auth-card`, `.branding-side`, `.brand-badge`, `.brand-title`, `.brand-subtitle`, `.brand-features`, `.brand-feature`, `.form-side` | Login, register, create-company |
| **Page header** | `.page-header` | All list pages |
| **Gradient button** | `.btn-gradient` | List page toolbars |
| **View toggle** | `.view-toggle` | Invoice/estimate list |
| **Table card** | `.table-card`, `.table-toolbar` | All table views |
| **Cards grid** | `.cards-grid` | Invoice/estimate card views |
| **Entity card** | `.entity-card`, `.entity-card-header`, `.entity-card-number`, `.entity-card-title`, `.entity-card-dates`, `.entity-card-actions` | Invoice/estimate card views |
| **Date/amount items** | `.date-item`, `.date-label`, `.date-value`, `.amount-item`, `.amount-label`, `.amount-value` | Cards |
| **Empty state** | `.empty-state` | List pages |
| **Form field** | `.form-grid`, `.form-field` | Dialogs |
| **Dialog footer** | `.dialog-footer` | All dialogs |
| **Image preview** | `.image-preview` | Logo/product image uploads |
| **Logo text** | `.logo-text` | Logo component |
| **Description cell** | `.description-cell` | Tables |

When adding new components, check `shared.css` first before writing new styles.

#### Frontend Conventions

- **Standalone components only** — no NgModules (except the legacy auto-generated `api.module.ts`)
- **File naming**: `component-name.ts`, `component-name.html`, `component-name.css` (no `.component` suffix)
- **Component structure**: `Components/{Feature}/{component-name}/` — each with `.ts`, `.html`, `.css`, `.spec.ts`
- **Styling**: Shared CSS (`src/styles/shared.css`) + Tailwind utility classes + PrimeNG component styles; plain CSS (no SCSS). Component CSS files should only contain styles unique to that component.
- **Routing**: Lazy-loaded routes preferred; SSR prerenders all routes
- **State**: Angular signals for component state
- **API base path**: `https://localhost:7261` (configured in generated `api/` code)

#### Adding a New Frontend Component

1. Generate: `ng generate component Components/{Feature}/{component-name}` (from `InvoicerClient/`)
2. The component is standalone by default in Angular 21
3. Import PrimeNG modules as needed (e.g., `ButtonModule`, `TableModule`)
4. Add route in `app.routes.ts`
5. Use generated API services for backend calls (inject directly)

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
- **Do not edit `InvoicerClient/src/app/api/`** — this directory is auto-generated by OpenAPI Generator; changes will be lost on next `npm run generate`
- **Angular 21 SSR**: all routes prerender by default (`RenderMode.Prerender` in `app.routes.server.ts`) — browser-only APIs (`window`, `document`) must be guarded with `isPlatformBrowser` or `afterNextRender`
- **PrimeNG 21 imports**: import component modules individually (e.g., `ButtonModule`, `InputOtpModule`), not the full PrimeNG package
