using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Company.UpdateCompanyDetails
{
    public readonly record struct UpdateCompanyDetailsCommand(
        [property: Required] Guid CompanyId,
        string? Name,
        string? Address,
        string? TaxNumber,
        string? PhoneNumber,
        string? Email,
        string? PaymentDetails,
        string? LogoUrl
    ) : IRequest;
}
