using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Client.UpdateClient
{
    public class UpdateClientHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<UpdateClientCommand>
    {
        public async Task Handle(UpdateClientCommand request, CancellationToken cancellationToken)
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(x => x.Companies)
                    .ThenInclude(c => c.Clients)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken: cancellationToken);

            if (user == null)
                throw new UserNotFoundException();
            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
            if (company == null)
                throw new CompanyNotFoundException();
            var client = company.Clients.FirstOrDefault(c => c.Id == request.ClientId);
            if (client == null)
                throw new ClientNotFoundException();
            if (request.Name is not null)
                client.Name = request.Name;
            if (request.Email is not null)
                client.Email = request.Email;
            if (request.Address is not null)
                client.Address = request.Address;
            if (request.TaxNumber is not null)
                client.TaxNumber = request.TaxNumber;
            if (request.PhoneNumber is not null)
                client.PhoneNumber = request.PhoneNumber;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
