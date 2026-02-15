using MediatR;

namespace Invoicer.Features.Invoice.GetAllInvoices
{
    public static class GetAllInvoicesEndpoint
    {
        public static IEndpointRouteBuilder MapGetAllInvoicesEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapGet(
                    "get-all-invoices/{CompanyId}",
                    async (Guid CompanyId, ISender sender) =>
                    {
                        var query = new GetAllInvoicesQuery(CompanyId);
                        var result = await sender.Send(query);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("GetAllInvoices")
                .RequireAuthorization();
            return app;
        }
    }
}
