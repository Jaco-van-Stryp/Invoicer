using MediatR;

namespace Invoicer.Features.Invoice.GetDashboardStats
{
    public readonly record struct GetDashboardStatsQuery(Guid CompanyId)
        : IRequest<GetDashboardStatsResponse>;
}
