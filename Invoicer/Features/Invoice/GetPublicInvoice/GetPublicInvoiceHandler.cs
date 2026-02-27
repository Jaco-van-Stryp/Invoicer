using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.GetPublicInvoice;

public class GetPublicInvoiceHandler(AppDbContext _dbContext)
    : IRequestHandler<GetPublicInvoiceQuery, GetPublicInvoiceResponse>
{
    public async Task<GetPublicInvoiceResponse> Handle(
        GetPublicInvoiceQuery request,
        CancellationToken cancellationToken
    )
    {
        var invoice = await _dbContext
            .Invoices.Include(i => i.Client)
            .Include(i => i.Company)
            .Include(i => i.Products)
                .ThenInclude(pi => pi.Product)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new InvoiceNotFoundException();
        }

        var totalPaid = invoice.Payments.Sum(p => p.Amount);
        var totalDue = invoice.Products.Sum(p => p.Quantity * p.Product.Price);

        return new GetPublicInvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.InvoiceDue,
            invoice.Status.ToString(),
            totalDue,
            null,
            new CompanyInfo(
                invoice.Company.Name,
                invoice.Company.Address,
                invoice.Company.PhoneNumber,
                invoice.Company.Email,
                invoice.Company.LogoUrl,
                invoice.Company.PaymentDetails
            ),
            new ClientInfo(
                invoice.Client.Name,
                invoice.Client.Email,
                invoice.Client.PhoneNumber,
                invoice.Client.Address
            ),
            invoice
                .Products.Select(pi => new ProductInfo(
                    pi.Product.Name,
                    pi.Quantity,
                    pi.Product.Price,
                    pi.Quantity * pi.Product.Price
                ))
                .ToList()
        );
    }
}
