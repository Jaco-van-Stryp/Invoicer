using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Features.Client.CreateClient;

public class CreateClientHandler(AppDbContext _dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<CreateClientCommand, CreateClientResponse>
{
    public async Task<CreateClientResponse> Handle(
        CreateClientCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.UserId;
        var user = await _dbContext
            .Users.Include(c => c.Companies)
                .ThenInclude(u => u.Clients)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
            throw new UserNotFoundException();
        var company = user.Companies.FirstOrDefault(c => c.Id == request.CompanyId);
        if (company == null)
            throw new CompanyNotFoundException();
        var client = company.Clients.FirstOrDefault(c => c.Email == request.Email);
        if (client != null)
            throw new ClientAlreadyExistsException();
        var newClient = new Domain.Entities.Client
        {
            Name = request.Name,
            Email = request.Email,
            Address = request.Address,
            TaxNumber = request.TaxNumber,
            PhoneNumber = request.PhoneNumber,
            CompanyId = request.CompanyId,
            Company = company,
        };
        _dbContext.Clients.Add(newClient);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var response = new CreateClientResponse(
            newClient.Id,
            newClient.Name,
            newClient.Email,
            newClient.Address,
            newClient.TaxNumber,
            newClient.PhoneNumber,
            newClient.CompanyId
        );
        return response;
    }
}
