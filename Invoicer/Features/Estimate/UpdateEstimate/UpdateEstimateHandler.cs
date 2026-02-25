using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Estimate.UpdateEstimate
{
    public class UpdateEstimateHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<UpdateEstimateCommand>
    {
        public async Task Handle(UpdateEstimateCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(u => u.Companies)
                    .ThenInclude(c => c.Estimates)
                        .ThenInclude(e => e.ProductEstimates)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Clients)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Products)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var estimate = company.Estimates.FirstOrDefault(e => e.Id == request.EstimateId);
            if (estimate == null)
                throw new EstimateNotFoundException();

            if (request.ClientId is not null)
            {
                var client = company.Clients.FirstOrDefault(c => c.Id == request.ClientId);
                if (client == null)
                    throw new ClientNotFoundException();
                estimate.ClientId = client.Id;
                estimate.Client = client;
            }
            if (request.EstimateDate is not null)
                estimate.EstimateDate = DateTime.SpecifyKind(
                    request.EstimateDate.Value,
                    DateTimeKind.Utc
                );
            if (request.ExpiresOn is not null)
                estimate.ExpiresOn = DateTime.SpecifyKind(
                    request.ExpiresOn.Value,
                    DateTimeKind.Utc
                );
            if (request.Status is not null)
                estimate.Status = request.Status.Value;
            if (request.Notes is not null)
                estimate.Notes = request.Notes;

            if (request.Products is not null)
            {
                _dbContext.ProductEstimates.RemoveRange(estimate.ProductEstimates);
                estimate.ProductEstimates.Clear();

                var newProductEstimates = new List<ProductEstimate>();
                foreach (var item in request.Products)
                {
                    var product = company.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product == null)
                        throw new ProductNotFoundException();

                    newProductEstimates.Add(
                        new ProductEstimate
                        {
                            ProductId = item.ProductId,
                            Product = product,
                            EstimateId = estimate.Id,
                            Estimate = estimate,
                            CompanyId = request.CompanyId,
                            Company = company,
                            Quantity = item.Quantity,
                            UnitPrice = product.Price,
                        }
                    );
                }

                foreach (var pe in newProductEstimates)
                    estimate.ProductEstimates.Add(pe);

                estimate.TotalAmount = newProductEstimates.Sum(pe => pe.Quantity * pe.UnitPrice);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
