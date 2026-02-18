using Invoicer.Domain.Entities;

namespace Invoicer.Features.Payment.RecordPayment
{
    public record struct RecordPaymentResponse(
        Guid PaymentId,
        decimal Amount,
        DateTime PaidOn,
        InvoiceStatus NewInvoiceStatus
    );
}
