using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Client.GetAllClients
{
    public class GetAllClientsHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetAllClientsQuery, List<GetAllClientsResponse>>
    {
        public async Task<List<GetAllClientsResponse>> Handle(
            GetAllClientsQuery request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;
            var user = await _dbContext
                .Users.Include(c => c.Companies)
                    .ThenInclude(c => c.Clients)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null)
                throw new UserNotFoundException();

            var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);

            if (company == null)
                throw new CompanyNotFoundException();

            var clients = company.Clients.ToList();
            var clientsResponse = clients
                .Select(c => new GetAllClientsResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Address = c.Address,
                    TaxNumber = c.TaxNumber,
                    PhoneNumber = c.PhoneNumber,
                })
                .ToList();
            return clientsResponse;
        }
    }
}
