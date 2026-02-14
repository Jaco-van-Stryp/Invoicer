using System;

namespace Invoicer.Features.Client.CreateClient;

public record struct CreateClientResponse(
    Guid Id,
    string Name,
    string Email,
    string? Address,
    string? TaxNumber,
    string? PhoneNumber,
    Guid CompanyId
);
