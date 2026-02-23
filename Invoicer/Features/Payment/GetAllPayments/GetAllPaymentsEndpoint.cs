using MediatR;

namespace Invoicer.Features.Payment.GetAllPayments
{
    public static class GetAllPaymentsEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllPaymentsEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "get-all-payments/{CompanyId}",
                    async (Guid CompanyId, ISender sender) =>
                    {
                        var query = new GetAllPaymentsQuery(CompanyId);
                        var result = await sender.Send(query);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("GetAllPayments")
                .RequireAuthorization();

            return app;
        }
    }
}
