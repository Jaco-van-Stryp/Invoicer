using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Client.DeleteClient
{
    public class DeleteClientHandler(
        AppDbContext _dbContext,
        ICurrentUserService _currentUserService
    ) : IRequestHandler<DeleteClientCommand>
    {
        public async Task Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(c => c.Companies)
                    .ThenInclude(c => c.Clients)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var client = company.Clients.FirstOrDefault(c => c.Id == request.ClientId);
            if (client == null)
                throw new ClientNotFoundException();
            _dbContext.Clients.Remove(client);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
