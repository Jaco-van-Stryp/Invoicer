using MediatR;

namespace Invoicer.Features.Payment.RecordPayment
{
    public static class RecordPaymentEndpoint
    {
        public static IEndpointRouteBuilder MapRecordPaymentEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "record-payment",
                    async (RecordPaymentCommand command, ISender sender) =>
                    {
                        var result = await sender.Send(command);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("RecordPayment")
                .RequireAuthorization();

            return app;
        }
    }
}
