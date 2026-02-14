using MediatR;

namespace Invoicer.Features.Invoice.UpdateInvoice
{
    public static class UpdateInvoiceEndpoint
    {
        public static IEndpointRouteBuilder MapUpdateInvoiceEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPatch(
                    "update-invoice",
                    async (UpdateInvoiceCommand command, ISender sender) =>
                    {
                        await sender.Send(command);
                        return TypedResults.Ok();
                    }
                )
                .WithName("UpdateInvoice")
                .RequireAuthorization();
            return app;
        }
    }
}
