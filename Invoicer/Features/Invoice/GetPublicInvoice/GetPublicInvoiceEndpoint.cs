using MediatR;

namespace Invoicer.Features.Invoice.GetPublicInvoice;

public static class GetPublicInvoiceEndpoint
{
    public static IEndpointRouteBuilder MapGetPublicInvoiceEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet(
                "public/{InvoiceId}",
                async (Guid InvoiceId, ISender sender) =>
                {
                    var query = new GetPublicInvoiceQuery(InvoiceId);
                    var result = await sender.Send(query);
                    return TypedResults.Ok(result);
                }
            )
            .WithName("GetPublicInvoice")
            .AllowAnonymous();

        return app;
    }
}
