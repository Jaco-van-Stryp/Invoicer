using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.UpdateInvoice
{
    public class UpdateInvoiceHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<UpdateInvoiceCommand>
    {
        public async Task Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Products)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Clients)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Products)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var invoice = company.Invoices.FirstOrDefault(i => i.Id == request.InvoiceId);
            if (invoice == null)
                throw new InvoiceNotFoundException();

            if (request.InvoiceNumber is not null)
                invoice.InvoiceNumber = request.InvoiceNumber;
            if (request.InvoiceDate is not null)
                invoice.InvoiceDate = request.InvoiceDate.Value;
            if (request.InvoiceDue is not null)
                invoice.InvoiceDue = request.InvoiceDue.Value;
            if (request.ClientId is not null)
            {
                var client = company.Clients.FirstOrDefault(c => c.Id == request.ClientId);
                if (client == null)
                    throw new ClientNotFoundException();
                invoice.ClientId = client.Id;
                invoice.Client = client;
            }

            if (request.Products is not null)
            {
                _dbContext.ProductInvoices.RemoveRange(invoice.Products);
                invoice.Products.Clear();

                foreach (var item in request.Products)
                {
                    var product = company.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product == null)
                        throw new ProductNotFoundException();

                    invoice.Products.Add(
                        new ProductInvoice
                        {
                            ProductId = item.ProductId,
                            Product = product,
                            InvoiceId = invoice.Id,
                            Invoice = invoice,
                            CompanyId = request.CompanyId,
                            Company = company,
                            Quantity = item.Quantity,
                        }
                    );
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
