using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Invoice.CreateInvoice
{
    public readonly record struct CreateInvoiceCommand(
        Guid CompanyId,
        Guid ClientId,
        DateTime InvoiceDate,
        DateTime InvoiceDue,
        [property: Required][property: MinLength(1)] List<CreateInvoiceProductItem> Products
    ) : IRequest<CreateInvoiceResponse>;

    public readonly record struct CreateInvoiceProductItem(
        Guid ProductId,
        [property: Range(1, int.MaxValue)] int Quantity
    );
}
