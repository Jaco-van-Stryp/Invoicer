using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Payment.GetAllPayments
{
    public class GetAllPaymentsHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetAllPaymentsQuery, List<GetAllPaymentsResponse>>
    {
        public async Task<List<GetAllPaymentsResponse>> Handle(
            GetAllPaymentsQuery request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var payments = await _dbContext
                .Payments.Where(p => p.CompanyId == request.CompanyId)
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Client)
                .OrderByDescending(p => p.PaidOn)
                .Select(p => new GetAllPaymentsResponse(
                    p.Id,
                    p.Amount,
                    p.PaidOn,
                    p.Notes,
                    p.InvoiceId,
                    p.Invoice.InvoiceNumber,
                    p.Invoice.Client.Name
                ))
                .ToListAsync(cancellationToken);

            return payments;
        }
    }
}
