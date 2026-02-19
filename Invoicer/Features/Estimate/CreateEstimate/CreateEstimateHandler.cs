using Invoicer.Domain.Data;
using Invoicer.Domain.Entities;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Estimate.CreateEstimate
{
    public class CreateEstimateHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<CreateEstimateCommand, CreateEstimateResponse>
    {
        public async Task<CreateEstimateResponse> Handle(
            CreateEstimateCommand request,
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

            var client = await _dbContext
                .Clients.FirstOrDefaultAsync(
                    c => c.Id == request.ClientId && c.CompanyId == request.CompanyId,
                    cancellationToken
                );

            if (client == null)
                throw new ClientNotFoundException();

            var productIds = request.Products.Select(p => p.ProductId).ToList();
            var products = await _dbContext
                .Products.Where(p =>
                    productIds.Contains(p.Id) && p.CompanyId == request.CompanyId
                )
                .ToListAsync(cancellationToken);

            if (products.Count != productIds.Count)
                throw new ProductNotFoundException();

            var estimateNumber = $"EST-{company.NextEstimateNumber:D4}";
            company.NextEstimateNumber++;

            var estimate = new Domain.Entities.Estimate
            {
                Id = Guid.NewGuid(),
                EstimateNumber = estimateNumber,
                EstimateDate = request.EstimateDate,
                ExpiresOn = request.ExpiresOn,
                Status = request.Status,
                Notes = request.Notes,
                ClientId = request.ClientId,
                Client = client,
                CompanyId = request.CompanyId,
                Company = company,
                ProductEstimates = [],
            };

            var productEstimates = request
                .Products.Select(p =>
                {
                    var product = products.First(pr => pr.Id == p.ProductId);
                    return new Domain.Entities.ProductEstimate
                    {
                        Id = Guid.NewGuid(),
                        EstimateId = estimate.Id,
                        Estimate = estimate,
                        ProductId = p.ProductId,
                        Product = product,
                        Quantity = p.Quantity,
                        UnitPrice = product.Price,
                        CompanyId = request.CompanyId,
                        Company = company,
                    };
                })
                .ToList();

            estimate.TotalAmount = productEstimates.Sum(pe => pe.Quantity * pe.UnitPrice);

            _dbContext.Estimates.Add(estimate);
            _dbContext.ProductEstimates.AddRange(productEstimates);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new CreateEstimateResponse(estimate.Id, estimate.EstimateNumber);
        }
    }
}
