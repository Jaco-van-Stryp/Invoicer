using Invoicer.Features.Company.CreateCompany;

namespace Invoicer.Features.Company
{
    public static class CompanyEndpoints
    {
        public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/company").WithTags("Company");

            group.MapCreateCompanyEndpoint();

            return app;
        }
    }
}
