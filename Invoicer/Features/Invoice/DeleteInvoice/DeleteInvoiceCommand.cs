namespace Invoicer.Features.Invoice.DeleteInvoice
{
    public readonly record struct DeleteInvoiceCommand(Guid CompanyId, Guid InvoiceId)
        : MediatR.IRequest;
}
