using MediatR;

namespace Invoicer.Features.Payment.DeletePayment
{
    public static class DeletePaymentEndpoint
    {
        public static IEndpointRouteBuilder MapDeletePaymentEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "delete-payment/{CompanyId}/{InvoiceId}/{PaymentId}",
                    async (Guid CompanyId, Guid InvoiceId, Guid PaymentId, ISender sender) =>
                    {
                        var command = new DeletePaymentCommand(CompanyId, InvoiceId, PaymentId);
                        await sender.Send(command);
                        return Results.NoContent();
                    }
                )
                .WithName("DeletePayment")
                .RequireAuthorization();

            return app;
        }
    }
}
