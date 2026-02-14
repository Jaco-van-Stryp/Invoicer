using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Client.CreateClient;

public readonly record struct CreateClientCommand(
    [property: Required] string Name,
    [property: Required] [property: EmailAddress] string Email,
    string? Address,
    string? TaxNumber,
    string? PhoneNumber,
    Guid CompanyId
) : IRequest<CreateClientResponse>;
