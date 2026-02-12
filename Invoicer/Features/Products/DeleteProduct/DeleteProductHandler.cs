using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Products.DeleteProduct
{
    public class DeleteProductHandler(
        AppDbContext _dbContext,
        ICurrentUserService _currentUserService
    ) : IRequestHandler<DeleteProductCommand>
    {
        public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(c => c.Companies)
                    .ThenInclude(p => p.Products)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new UserNotFoundException();
            var comapny = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (comapny == null)
                throw new CompanyNotFoundException();
            var product = comapny.Products.FirstOrDefault(p => p.Id == request.ProductId);
            if (product == null)
                throw new ProductNotFoundException();
            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
