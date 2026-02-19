namespace Invoicer.Features.Payment.GetAllPayments
{
    public record GetAllPaymentsResponse(
        Guid Id,
        decimal Amount,
        DateTime PaidOn,
        string? Notes,
        Guid InvoiceId,
        string InvoiceNumber,
        string ClientName
    );
}
