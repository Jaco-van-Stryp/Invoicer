namespace Invoicer.Features.Invoice.GetAllInvoices
{
    public record struct GetAllInvoicesResponse(
        Guid Id,
        string InvoiceNumber,
        DateTime InvoiceDate,
        DateTime InvoiceDue,
        Guid ClientId,
        string ClientName,
        List<InvoiceProductItem> Products
    );

    public record struct InvoiceProductItem(
        Guid ProductId,
        string ProductName,
        decimal Price,
        int Quantity
    );
}
