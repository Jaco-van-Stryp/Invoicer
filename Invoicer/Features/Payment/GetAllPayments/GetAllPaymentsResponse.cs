namespace Invoicer.Features.Payment.GetAllPayments
{
    public record struct GetAllPaymentsResponse(
        Guid Id,
        decimal Amount,
        DateTime PaidOn,
        string? Notes,
        Guid InvoiceId,
        string InvoiceNumber,
        string ClientName
    );
}
