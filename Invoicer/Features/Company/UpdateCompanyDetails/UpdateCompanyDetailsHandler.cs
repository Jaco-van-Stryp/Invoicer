using Invoicer.Domain.Data;
using Invoicer.Domain.Exceptions;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Invoicer.Features.Company.UpdateCompanyDetails
{
    public class UpdateCompanyDetailsHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<UpdateCompanyDetailsCommand>
    {
        public async Task Handle(
            UpdateCompanyDetailsCommand request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;

            var user = await _dbContext
                .Users.Include(x => x.Companies)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null)
            {
                Log.Warning("Unauthorized attempt to update a company by UserId {UserId}", userId);
                throw new UserNotFoundException();
            }
            var company = user.Companies.FirstOrDefault(x => x.Id == request.CompanyId);
            if (company == null)
            {
                Log.Warning(
                    "User {UserId} requested a company that does not belong to them",
                    userId
                );
                throw new CompanyNotFoundException();
            }
            Log.Information("Updating Company {CompanyId}", company.Id);

            if (request.Name is not null)
                company.Name = request.Name;
            if (request.Address is not null)
                company.Address = request.Address;
            if (request.PhoneNumber is not null)
                company.PhoneNumber = request.PhoneNumber;
            if (request.Email is not null)
                company.Email = request.Email;
            if (request.TaxNumber is not null)
                company.TaxNumber = request.TaxNumber;
            if (request.PaymentDetails is not null)
                company.PaymentDetails = request.PaymentDetails;
            if (request.LogoUrl is not null)
                company.LogoUrl = request.LogoUrl;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
