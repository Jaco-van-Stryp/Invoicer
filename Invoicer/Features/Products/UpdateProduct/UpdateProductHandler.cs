using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Products.UpdateProduct
{
    public class UpdateProductHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<UpdateProductCommand>
    {
        public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(x => x.Companies)
                    .ThenInclude(c => c.Products)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var product = company.Products.FirstOrDefault(x => x.Id == request.ProductId);
            if (product == null)
                throw new ProductNotFoundException();
            if (request.Name is not null)
                product.Name = request.Name;
            if (request.Description is not null)
                product.Description = request.Description;
            if (request.Price is not null)
                product.Price = request.Price.Value;
            if (request.ImageUrl is not null)
                product.ImageUrl = request.ImageUrl;

            _dbContext.Products.Update(product);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
