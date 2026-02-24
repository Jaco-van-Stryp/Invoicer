using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Invoice.SendInvoiceEmail;

public readonly record struct SendInvoiceEmailCommand(Guid InvoiceId, Guid CompanyId) : IRequest;
