using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Estimate.GetAllEstimates
{
    public class GetAllEstimatesHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetAllEstimatesQuery, List<GetAllEstimatesResponse>>
    {
        public async Task<List<GetAllEstimatesResponse>> Handle(
            GetAllEstimatesQuery request,
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

            var estimates = await _dbContext
                .Estimates.Where(e => e.CompanyId == request.CompanyId)
                .Include(e => e.Client)
                .Include(e => e.ProductEstimates)
                    .ThenInclude(pe => pe.Product)
                .OrderByDescending(e => e.EstimateDate)
                .ToListAsync(cancellationToken);

            var estimateResponses = estimates
                .Select(e =>
                {
                    var subtotal = e.ProductEstimates.Sum(pe => pe.UnitPrice * pe.Quantity);
                    var taxableSubtotal = e
                        .ProductEstimates.Where(pe => pe.IsTaxed)
                        .Sum(pe => pe.UnitPrice * pe.Quantity);
                    var taxAmount = Math.Round(taxableSubtotal * (e.TaxRate / 100), 2);
                    return new GetAllEstimatesResponse(
                        e.Id,
                        e.EstimateNumber,
                        e.EstimateDate,
                        e.ExpiresOn,
                        e.Status,
                        subtotal + taxAmount,
                        subtotal,
                        taxAmount,
                        e.TaxRate,
                        e.TaxName,
                        e.Notes,
                        e.ClientId,
                        e.Client.Name,
                        e.ProductEstimates.Select(pe => new EstimateProductItem(
                                pe.ProductId,
                                pe.Product.Name,
                                pe.Quantity,
                                pe.UnitPrice,
                                pe.IsTaxed
                            ))
                            .ToList()
                    );
                })
                .ToList();

            return estimateResponses;
        }
    }
}
