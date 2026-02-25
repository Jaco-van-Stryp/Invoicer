# Invoicer

A full-stack invoicing and billing management app for freelancers and small businesses. Manage clients, products, invoices, estimates, and payments — with income analytics and email delivery built in.

## Features

- **Multi-company support** — one account, multiple companies
- **Clients & products** — maintain a client directory and reusable product/service catalog
- **Invoices** — create, edit, send via email, and track payment status (unpaid / partial / paid)
- **Estimates** — create and manage quotes that can be sent to clients
- **Payments** — record partial or full payments against invoices
- **Dashboard** — monthly income trends, income by client, and invoice status breakdown
- **File storage** — upload company logos and product images (MinIO / S3-compatible)
- **Passwordless auth** — sign in with a 6-digit email code (AWS SES); no passwords

## Tech Stack

| Layer    | Technology                                                  |
| -------- | ----------------------------------------------------------- |
| Backend  | .NET 10 Web API · Minimal APIs · MediatR (CQRS) · EF Core  |
| Database | PostgreSQL (via Npgsql)                                     |
| Frontend | Angular 21 · SSR · PrimeNG 21 · Tailwind CSS 4             |
| Desktop  | Electron (optional desktop wrapper)                         |
| Storage  | MinIO (S3-compatible)                                       |
| Email    | AWS SES v2                                                  |

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js + npm
- Docker (PostgreSQL via Testcontainers for tests; MinIO; OpenAPI code generation)
- AWS SES credentials (for email)

### Backend

```bash
# Configure appsettings.json with DB, JWT, SES, and MinIO settings
dotnet run --project Invoicer        # API runs on https://localhost:7261
```

### Frontend

```bash
cd InvoicerClient
npm install
npm start                            # Dev server on http://localhost:4200
```

### Running Tests

```bash
dotnet test Invoicer.Tests           # Requires Docker (spins up a PostgreSQL container)
```
