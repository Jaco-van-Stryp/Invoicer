using MediatR;

namespace Invoicer.Features.Invoice.CreateInvoice
{
    public static class CreateInvoiceEndpoint
    {
        public static IEndpointRouteBuilder MapCreateInvoiceEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "create-invoice",
                    async (CreateInvoiceCommand command, ISender sender) =>
                    {
                        var result = await sender.Send(command);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("CreateInvoice")
                .RequireAuthorization();
            return app;
        }
    }
}
