using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Payment.RecordPayment
{
    public readonly record struct RecordPaymentCommand(
        Guid CompanyId,
        Guid InvoiceId,
        decimal Amount,
        DateTime PaidOn,
        string? Notes
    ) : IRequest<RecordPaymentResponse>;
}
