using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.CreateInvoice
{
    public class CreateInvoiceHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResponse>
    {
        public async Task<CreateInvoiceResponse> Handle(
            CreateInvoiceCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(c => c.Clients)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Products)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var client = company.Clients.FirstOrDefault(c => c.Id == request.ClientId);
            if (client == null)
                throw new ClientNotFoundException();

            var invoiceNumber = $"INV-{company.NextInvoiceNumber:D4}";
            company.NextInvoiceNumber++;

            var invoice = new Domain.Entities.Invoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = request.InvoiceDate,
                InvoiceDue = request.InvoiceDue,
                ClientId = request.ClientId,
                Client = client,
                CompanyId = request.CompanyId,
                Company = company,
                Products = new List<ProductInvoice>(),
            };

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

            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateInvoiceResponse(invoice.Id, invoice.InvoiceNumber);
        }
    }
}
