using MediatR;

namespace Invoicer.Features.Estimate.GetAllEstimates
{
    public static class GetAllEstimatesEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllEstimatesEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "get-all-estimates/{CompanyId}",
                    async (Guid CompanyId, ISender sender) =>
                    {
                        var query = new GetAllEstimatesQuery(CompanyId);
                        var result = await sender.Send(query);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("GetAllEstimates")
                .RequireAuthorization();

            return app;
        }
    }
}
