using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Company.CreateCompany
{
    public readonly record struct CreateCompanyCommand(
        string Name,
        string? Address,
        string? TaxNumber,
        string? PhoneNumber,
        string? Email,
        string? PaymentDetails,
        string? LogoUrl
    ) : IRequest<CreateCompanyResponse>;
}
