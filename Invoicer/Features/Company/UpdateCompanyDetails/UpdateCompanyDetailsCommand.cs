using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Company.UpdateCompanyDetails
{
    public readonly record struct UpdateCompanyDetailsCommand(
        Guid CompanyId,
        string? Name,
        string? Address,
        string? TaxNumber,
        string? PhoneNumber,
        string? Email,
        string? PaymentDetails,
        string? LogoUrl,
        [Range(typeof(decimal), "0", "100")] decimal? TaxRate,
        [MaxLength(100)] string? TaxName
    ) : IRequest;
}
