using MediatR;

namespace Invoicer.Features.Invoice.SendInvoiceEmail;

public static class SendInvoiceEmailEndpoint
{
    public static IEndpointRouteBuilder MapSendInvoiceEmailEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "{InvoiceId}/send-email",
                async (Guid InvoiceId, SendInvoiceEmailRequest req, ISender sender) =>
                {
                    var command = new SendInvoiceEmailCommand(InvoiceId, req.CompanyId);
                    await sender.Send(command);
                    return TypedResults.Ok();
                }
            )
            .WithName("SendInvoiceEmail")
            .RequireAuthorization();

        return app;
    }
}

public record SendInvoiceEmailRequest(Guid CompanyId);
