namespace Invoicer.Features.Invoice.UpdateInvoice
{
    public readonly record struct UpdateInvoiceCommand(
        Guid CompanyId,
        Guid InvoiceId,
        string? InvoiceNumber,
        DateTime? InvoiceDate,
        DateTime? InvoiceDue,
        Guid? ClientId,
        List<UpdateInvoiceProductItem>? Products
    ) : MediatR.IRequest;

    public readonly record struct UpdateInvoiceProductItem(Guid ProductId, int Quantity);
}
