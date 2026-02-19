using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicer.Features.Invoice.GetPublicInvoice;

public static class GetPublicInvoiceEndpoint
{
    public static RouteGroupBuilder MapGetPublicInvoiceEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet(
                "/public/{invoiceId:guid}",
                async ([FromRoute] Guid invoiceId, ISender sender) =>
                {
                    var query = new GetPublicInvoiceQuery(invoiceId);
                    var result = await sender.Send(query);
                    return Results.Ok(result);
                }
            )
            .WithName("GetPublicInvoice")
            .WithTags("Invoice")
            .Produces<GetPublicInvoiceResponse>()
            .AllowAnonymous();

        return group;
    }
}
