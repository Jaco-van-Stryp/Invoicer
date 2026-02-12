using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Features.Products.GetProducts;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Products.GetAllProducts
{
    public class GetAllProductsHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetAllProductsQuery, List<GetAllProductsResponse>>
    {
        public async Task<List<GetAllProductsResponse>> Handle(
            GetAllProductsQuery request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(c => c.Companies)
                    .ThenInclude(p => p.Products)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var products = company.Products.ToList();
            var productsResponse = products
                .Select(p => new GetAllProductsResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                })
                .ToList();
            return productsResponse;
        }
    }
}
