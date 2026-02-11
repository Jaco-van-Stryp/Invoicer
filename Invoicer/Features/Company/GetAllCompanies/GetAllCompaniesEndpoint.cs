using MediatR;

namespace Invoicer.Features.Company.GetAllCompanies
{
    public static class GetAllCompaniesEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllCompaniesEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "get-all-companies",
                    async (ISender sender) =>
                    {
                        var query = new GetAllCompaniesQuery();
                        var result = await sender.Send(query);
                        return Results.Ok(result);
                    }
                )
                .WithName("GetAllCompanies")
                .RequireAuthorization();
            return app;
        }
    }
}
