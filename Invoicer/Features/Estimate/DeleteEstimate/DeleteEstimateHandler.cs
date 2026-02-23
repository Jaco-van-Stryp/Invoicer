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
    ) : IRequestHandler<DeleteEstimateCommand>
    {
        public async Task Handle(DeleteEstimateCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(e => e.Estimates)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var estimate = user
                .Companies.SelectMany(c => c.Estimates)
                .FirstOrDefault(e => e.Id == request.EstimateId);

            if (estimate == null)
                throw new EstimateNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == estimate.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            _dbContext.Estimates.Remove(estimate);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
