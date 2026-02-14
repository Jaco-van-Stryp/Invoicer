using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.DeleteInvoice
{
    public class DeleteInvoiceHandler(
        AppDbContext _dbContext,
        ICurrentUserService _currentUserService
    ) : IRequestHandler<DeleteInvoiceCommand>
    {
        public async Task Handle(DeleteInvoiceCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(c => c.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Products)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var invoice = company.Invoices.FirstOrDefault(i => i.Id == request.InvoiceId);
            if (invoice == null)
                throw new InvoiceNotFoundException();
            _dbContext.ProductInvoices.RemoveRange(invoice.Products);
            _dbContext.Invoices.Remove(invoice);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
