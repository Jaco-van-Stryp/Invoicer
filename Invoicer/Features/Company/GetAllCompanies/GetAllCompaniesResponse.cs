namespace Invoicer.Features.Company.GetAllCompanies
{
    public record struct GetAllCompaniesResponse(
        Guid Id,
        string Name,
        string Address,
        string TaxNumber,
        string PhoneNumber,
        string Email,
        string PaymentDetails,
        string LogoUrl
    );
}
