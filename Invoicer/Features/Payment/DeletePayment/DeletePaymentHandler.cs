using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Payment.DeletePayment
{
    public class DeletePaymentHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<DeletePaymentCommand>
    {
        public async Task Handle(DeletePaymentCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Products)
                            .ThenInclude(pi => pi.Product)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Invoices)
                        .ThenInclude(i => i.Payments)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var invoice = company.Invoices.FirstOrDefault(i => i.Id == request.InvoiceId);
            if (invoice == null)
                throw new InvoiceNotFoundException();

            var payment = invoice.Payments.FirstOrDefault(p => p.Id == request.PaymentId);
            if (payment == null)
                throw new PaymentNotFoundException();

            invoice.Payments.Remove(payment);
            _dbContext.Payments.Remove(payment);

            var totalDue = invoice.Products.Sum(pi => pi.Product.Price * pi.Quantity);
            var totalPaid = invoice.Payments.Sum(p => p.Amount);
            invoice.Status = Domain.Entities.Invoice.ComputeStatus(totalDue, totalPaid);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
