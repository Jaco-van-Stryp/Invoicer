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
                    .ThenInclude(c => c.Clients)
                .Include(u => u.Companies)
                    .ThenInclude(c => c.Products)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var client = company.Clients.FirstOrDefault(c => c.Id == request.ClientId);

            if (client == null)
                throw new ClientNotFoundException();

            var productIds = request.Products.Select(p => p.ProductId).ToList();
            var products = company.Products.Where(p => productIds.Contains(p.Id)).ToList();

            if (products.Count != productIds.Count)
                throw new ProductNotFoundException();

            var estimateNumber = $"EST-{company.NextEstimateNumber:D4}";
            company.NextEstimateNumber++;

            var estimate = new Domain.Entities.Estimate
            {
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
                    return new ProductEstimate
                    {
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
