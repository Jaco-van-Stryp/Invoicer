using Invoicer.Features.Payment.DeletePayment;
using Invoicer.Features.Payment.RecordPayment;

namespace Invoicer.Features.Payment
{
    public static class PaymentEndpoints
    {
        public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/payment").WithTags("Payment");

            group.MapRecordPaymentEndpoint();
            group.MapDeletePaymentEndpoint();

            return app;
        }
    }
}
