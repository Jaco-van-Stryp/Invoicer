using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.GetAllInvoices
{
    public class GetAllInvoicesHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetAllInvoicesQuery, List<GetAllInvoicesResponse>>
    {
        public async Task<List<GetAllInvoicesResponse>> Handle(
            GetAllInvoicesQuery request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Client)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Products)
                            .ThenInclude(pi => pi.Product)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var invoicesResponse = company
                .Invoices.Select(i => new GetAllInvoicesResponse
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    InvoiceDue = i.InvoiceDue,
                    ClientId = i.ClientId,
                    ClientName = i.Client.Name,
                    Status = i.Status,
                    TotalDue = i.Products.Sum(pi => pi.Product.Price * pi.Quantity),
                    TotalPaid = i.Payments.Sum(p => p.Amount),
                    Products = i
                        .Products.Select(pi => new InvoiceProductItem
                        {
                            ProductId = pi.ProductId,
                            ProductName = pi.Product.Name,
                            Price = pi.Product.Price,
                            Quantity = pi.Quantity,
                        })
                        .ToList(),
                    Payments = i
                        .Payments.Select(p => new InvoicePaymentItem
                        {
                            PaymentId = p.Id,
                            Amount = p.Amount,
                            PaidOn = p.PaidOn,
                            Notes = p.Notes,
                        })
                        .ToList(),
                })
                .ToList();

            return invoicesResponse;
        }
    }
}
