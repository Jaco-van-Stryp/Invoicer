using Invoicer.Domain.Data;
using Invoicer.Infrastructure.CurrentUserService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Invoicer.Features.Company.GetAllCompanies
{
    public class GetAllCompaniesHandler(
        AppDbContext _dbContext,
        ICurrentUserService currentUserService
    ) : IRequestHandler<GetAllCompaniesQuery, List<GetAllCompaniesResponse>>
    {
        public async Task<List<GetAllCompaniesResponse>> Handle(
            GetAllCompaniesQuery request,
            CancellationToken cancellationToken
        )
        {
            var userId = currentUserService.UserId;

            var companies = await _dbContext
                .Companies.Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            Log.Information(
                "Retrieved {Count} companies for user {UserId}",
                companies.Count,
                userId
            );
            var res = new List<GetAllCompaniesResponse>();
            foreach (var company in companies)
            {
                res.Add(
                    new GetAllCompaniesResponse
                    {
                        Id = company.Id,
                        Name = company.Name,
                        Address = company.Address,
                        TaxNumber = company.TaxNumber,
                        PhoneNumber = company.PhoneNumber,
                        Email = company.Email,
                        PaymentDetails = company.PaymentDetails,
                        LogoUrl = company.LogoUrl,
                    }
                );
            }
            return res;
        }
    }
}
