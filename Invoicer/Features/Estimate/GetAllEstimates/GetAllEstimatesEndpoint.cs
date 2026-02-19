using MediatR;

namespace Invoicer.Features.Estimate.GetAllEstimates
{
    public static class GetAllEstimatesEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllEstimatesEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "all-estimates",
                    async (Guid companyId, ISender sender) =>
                    {
                        var query = new GetAllEstimatesQuery(companyId);
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
