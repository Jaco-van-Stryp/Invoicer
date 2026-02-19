using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Estimate.DeleteEstimate
{
    public class DeleteEstimateHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<DeleteEstimateCommand, Unit>
    {
        public async Task<Unit> Handle(
            DeleteEstimateCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var estimate = await _dbContext
                .Estimates.FirstOrDefaultAsync(e => e.Id == request.EstimateId, cancellationToken);

            if (estimate == null)
                throw new EstimateNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == estimate.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            _dbContext.Estimates.Remove(estimate);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
