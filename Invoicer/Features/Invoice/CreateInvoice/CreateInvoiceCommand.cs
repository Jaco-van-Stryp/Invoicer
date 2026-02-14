using MediatR;

namespace Invoicer.Features.Invoice.CreateInvoice
{
    public readonly record struct CreateInvoiceCommand(
        Guid CompanyId,
        Guid ClientId,
        string InvoiceNumber,
        DateTime InvoiceDate,
        DateTime InvoiceDue,
        List<CreateInvoiceProductItem> Products
    ) : IRequest<CreateInvoiceResponse>;

    public readonly record struct CreateInvoiceProductItem(Guid ProductId, int Quantity);
}
