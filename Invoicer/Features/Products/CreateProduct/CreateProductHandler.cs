using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Products.CreateProduct
{
    public class CreateProductHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<CreateProductCommand, CreateProductResponse>
    {
        public async Task<CreateProductResponse> Handle(
            CreateProductCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(x => x.Companies)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);
            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();

            var product = new Domain.Entities.Product
            {
                Company = company,
                CompanyId = company.Id,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                Name = request.Name,
                Price = request.Price,
            };

            await _dbContext.Products.AddAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new CreateProductResponse
            {
                CompanyId = product.CompanyId,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Name = product.Name,
                Price = product.Price,
                ProductId = product.Id,
            };
            return response;
        }
    }
}
