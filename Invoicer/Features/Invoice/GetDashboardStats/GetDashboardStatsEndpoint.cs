using MediatR;

namespace Invoicer.Features.Invoice.GetDashboardStats
{
    public static class GetDashboardStatsEndpoint
    {
        public static IEndpointRouteBuilder MapGetDashboardStatsEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "get-dashboard-stats/{CompanyId}",
                    async (Guid CompanyId, ISender sender) =>
                    {
                        var query = new GetDashboardStatsQuery(CompanyId);
                        var result = await sender.Send(query);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("GetDashboardStats")
                .RequireAuthorization();

            return app;
        }
    }
}
