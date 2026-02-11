using Invoicer.Features.Company.CreateCompany;
using Invoicer.Features.Company.GetAllCompanies;
using Invoicer.Features.Company.UpdateCompanyDetails;

namespace Invoicer.Features.Company
{
    public static class CompanyEndpoints
    {
        public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/company").WithTags("Company");

            group.MapCreateCompanyEndpoint();
            group.MapGetAllCompaniesEndpoint();
            group.MapUpdateCompanyDetailsEndpoint();
            // TODO - allow deleting companies in the future, for now that's not allowed.

            return app;
        }
    }
}
