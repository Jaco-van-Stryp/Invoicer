using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Payment.RecordPayment
{
    public readonly record struct RecordPaymentCommand(
        Guid CompanyId,
        Guid InvoiceId,
        [property: Range(0.01, double.MaxValue)] decimal Amount,
        DateTime PaidOn,
        string? Notes
    ) : IRequest<RecordPaymentResponse>;
}
