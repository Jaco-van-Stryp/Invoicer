using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Invoice.GetPublicInvoice;

public readonly record struct GetPublicInvoiceQuery(
    [Required] Guid InvoiceId
) : IRequest<GetPublicInvoiceResponse>;
