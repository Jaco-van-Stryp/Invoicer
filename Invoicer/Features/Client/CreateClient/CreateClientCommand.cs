using MediatR;

namespace Invoicer.Features.Client.CreateClient;

public readonly record struct CreateClientCommand(
    string Name,
    string Email,
    string? Address,
    string? TaxNumber,
    string? PhoneNumber,
    Guid CompanyId
) : IRequest<CreateClientResponse>;
