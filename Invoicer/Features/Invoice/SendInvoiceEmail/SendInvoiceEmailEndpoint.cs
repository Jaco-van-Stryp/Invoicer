using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Invoicer.Features.Invoice.SendInvoiceEmail;

public static class SendInvoiceEmailEndpoint
{
    public static RouteGroupBuilder MapSendInvoiceEmailEndpoint(this RouteGroupBuilder group)
    {
        group
            .MapPost(
                "/{invoiceId:guid}/send-email",
                async ([FromRoute] Guid invoiceId, [FromBody] SendInvoiceEmailRequest req, ISender sender) =>
                {
                    var command = new SendInvoiceEmailCommand(invoiceId, req.CompanyId);
                    await sender.Send(command);
                    return Results.Ok();
                }
            )
            .WithName("SendInvoiceEmail")
            .WithTags("Invoice")
            .RequireAuthorization();

        return group;
    }
}

public record SendInvoiceEmailRequest(Guid CompanyId);
