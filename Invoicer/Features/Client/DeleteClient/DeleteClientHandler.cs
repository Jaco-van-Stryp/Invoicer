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
            // TODO  - might have to mark a client as deleted instead of actually deleting it, if there are invoices linked to it. For now, just prevent deletion if there are invoices.
            var hasInvoices = await _dbContext.Invoices.AnyAsync(
                i => i.ClientId == client.Id,
                cancellationToken
            );

            if (hasInvoices)
                throw new ClientHasInvoicesException();

            _dbContext.Clients.Remove(client);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
