using Invoicer.Features.Company.GetAllCompanies;
using MediatR;

namespace Invoicer.Features.Products.GetAllProducts
{
    public static class GetAllProductsEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllProductsEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "get-all-products/{CompanyId}",
                    async (ISender sender) =>
                    {
                        var query = new GetAllProductsQuery(companyId);
                        var result = await sender.Send(query);
                        return Results.Ok(result);
                    }
                )
                .WithName("GetAllProducts")
                .RequireAuthorization();
            return app;
        }
    }
}
