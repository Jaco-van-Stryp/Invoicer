using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Payment.RecordPayment
{
    public class RecordPaymentHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<RecordPaymentCommand, RecordPaymentResponse>
    {
        public async Task<RecordPaymentResponse> Handle(
            RecordPaymentCommand request,
            CancellationToken cancellationToken
        )
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

            var payment = new Domain.Entities.Payment
            {
                Amount = request.Amount,
                PaidOn = DateTime.SpecifyKind(request.PaidOn, DateTimeKind.Utc),
                Notes = request.Notes,
                InvoiceId = invoice.Id,
                Invoice = invoice,
                CompanyId = request.CompanyId,
                Company = company,
            };

            invoice.Payments.Add(payment);

            var totalDue = invoice.Products.Sum(pi => pi.Product.Price * pi.Quantity);
            var totalPaid = invoice.Payments.Sum(p => p.Amount);
            invoice.Status = Domain.Entities.Invoice.ComputeStatus(totalDue, totalPaid);

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new RecordPaymentResponse(payment.Id, payment.Amount, payment.PaidOn, invoice.Status);
        }
    }
}
