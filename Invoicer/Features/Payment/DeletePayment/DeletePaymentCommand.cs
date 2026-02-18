using MediatR;

namespace Invoicer.Features.Payment.DeletePayment
{
    public readonly record struct DeletePaymentCommand(
        Guid CompanyId,
        Guid InvoiceId,
        Guid PaymentId
    ) : IRequest;
}
