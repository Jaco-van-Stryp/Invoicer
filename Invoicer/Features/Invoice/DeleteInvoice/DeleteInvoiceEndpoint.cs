using MediatR;

namespace Invoicer.Features.Invoice.DeleteInvoice
{
    public static class DeleteInvoiceEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteInvoiceEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "delete-invoice/{CompanyId}/{InvoiceId}",
                    async (Guid CompanyId, Guid InvoiceId, ISender sender) =>
                    {
                        var command = new DeleteInvoiceCommand(CompanyId, InvoiceId);
                        await sender.Send(command);
                        return Results.NoContent();
                    }
                )
                .WithName("DeleteInvoice")
                .RequireAuthorization();
            return app;
        }
    }
}
