using Invoicer.Domain.Enums;

namespace Invoicer.Features.Invoice.GetAllInvoices
{
    public record struct GetAllInvoicesResponse(
        Guid Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        DateTime InvoiceDue,
        Guid ClientId,
        string ClientName,
        InvoiceStatus Status,
        decimal TotalDue,
        decimal TotalPaid,
        List<InvoiceProductItem> Products,
        List<InvoicePaymentItem> Payments
    );

    public record struct InvoiceProductItem(
        Guid ProductId,
        string ProductName,
        decimal Price,
        int Quantity
    );

    public record struct InvoicePaymentItem(
        Guid PaymentId,
        decimal Amount,
        DateTime PaidOn,
        string? Notes
    );
}
