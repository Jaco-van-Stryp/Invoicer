using MediatR;

namespace Invoicer.Features.Payment.GetAllPayments
{
    public static class GetAllPaymentsEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllPaymentsEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "/all-payments",
                    async (Guid companyId, ISender sender, CancellationToken ct) =>
                    {
                        var query = new GetAllPaymentsQuery(companyId);
                        var result = await sender.Send(query, ct);
                        return Results.Ok(result);
                    }
                )
                .WithName("GetAllPayments")
                .RequireAuthorization();

            return app;
        }
    }
}
