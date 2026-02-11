using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Invoicer.Features.Company.CreateCompany
{
    public class CreateCompanyHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<CreateCompanyCommand, CreateCompanyResponse>
    {
        public async Task<CreateCompanyResponse> Handle(
            CreateCompanyCommand request,
            CancellationToken cancellationToken
        )
        {
            var currentUserId = currentUserService.UserId;

            var user = await _dbContext.Users.FirstOrDefaultAsync(
                x => x.Id == currentUserId,
                cancellationToken
            );

            if (user == null)
            {
                Log.Error(
                    "User with ID {UserId} not found when trying to create a company.",
                    currentUserId
                );
                throw new UserNotFoundException();
            }

            Log.Information(
                "User with ID {UserId} is creating a company called {CompanyName}.",
                currentUserId,
                request.Name
            );

            var company = new Domain.Entities.Company
            {
                Address = request.Address ?? string.Empty,
                Email = request.Email ?? string.Empty,
                LogoUrl = request.LogoUrl ?? string.Empty,
                Name = request.Name,
                PaymentDetails = request.PaymentDetails ?? string.Empty,
                PhoneNumber = request.PhoneNumber ?? string.Empty,
                TaxNumber = request.TaxNumber ?? string.Empty,
                UserId = user.Id,
                User = user,
            };

            await _dbContext.Companies.AddAsync(company, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            var response = new CreateCompanyResponse(company.Id);
            return response;
        }
    }
}
