using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Invoice.GetPublicInvoice;

public readonly record struct GetPublicInvoiceQuery(Guid InvoiceId)
    : IRequest<GetPublicInvoiceResponse>;
