using MediatR;

namespace Invoicer.Features.Client.GetAllClients
{
    public readonly record struct GetAllClientsQuery(Guid CompanyId)
        : IRequest<List<GetAllClientsResponse>>;
}
