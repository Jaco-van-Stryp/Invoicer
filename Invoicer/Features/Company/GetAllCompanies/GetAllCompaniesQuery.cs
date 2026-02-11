using MediatR;

namespace Invoicer.Features.Company.GetAllCompanies
{
    public readonly record struct GetAllCompaniesQuery()
        : IRequest<List<GetAllCompaniesResponse>>;
}
