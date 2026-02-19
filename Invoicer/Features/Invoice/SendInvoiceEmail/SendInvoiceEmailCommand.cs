using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Invoice.SendInvoiceEmail;

public readonly record struct SendInvoiceEmailCommand(
    [Required] Guid InvoiceId,
    [Required] Guid CompanyId
) : IRequest;
