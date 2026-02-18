using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Invoice.GetDashboardStats
{
    public class GetDashboardStatsHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetDashboardStatsQuery, GetDashboardStatsResponse>
    {
        public async Task<GetDashboardStatsResponse> Handle(
            GetDashboardStatsQuery request,
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

            var monthlyIncome = await _dbContext
                .Payments.Where(p => p.CompanyId == request.CompanyId)
                .GroupBy(p => new { p.PaidOn.Year, p.PaidOn.Month })
                .Select(g => new MonthlyIncomeItem(g.Key.Year, g.Key.Month, g.Sum(p => p.Amount)))
                .OrderBy(m => m.Year)
                .ThenBy(m => m.Month)
                .ToListAsync(cancellationToken);

            var incomeByClient = await _dbContext
                .Payments.Where(p => p.CompanyId == request.CompanyId)
                .GroupBy(p => new { p.Invoice.ClientId, p.Invoice.Client.Name })
                .Select(g => new ClientIncomeItem(g.Key.ClientId, g.Key.Name, g.Sum(p => p.Amount)))
                .OrderByDescending(c => c.TotalPaid)
                .ToListAsync(cancellationToken);

            var statusCounts = await _dbContext
                .Invoices.Where(i => i.CompanyId == request.CompanyId)
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var statusSummary = new InvoiceStatusSummary(
                PaidCount: statusCounts
                    .FirstOrDefault(s => s.Status == InvoiceStatus.Paid)
                    ?.Count ?? 0,
                PartialCount: statusCounts
                    .FirstOrDefault(s => s.Status == InvoiceStatus.Partial)
                    ?.Count ?? 0,
                UnpaidCount: statusCounts
                    .FirstOrDefault(s => s.Status == InvoiceStatus.Unpaid)
                    ?.Count ?? 0
            );

            return new GetDashboardStatsResponse(monthlyIncome, incomeByClient, statusSummary);
        }
    }
}
