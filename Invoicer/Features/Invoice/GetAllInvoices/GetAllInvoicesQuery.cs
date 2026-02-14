using MediatR;

namespace Invoicer.Features.Invoice.GetAllInvoices
{
    public readonly record struct GetAllInvoicesQuery(Guid CompanyId)
        : IRequest<List<GetAllInvoicesResponse>>;
}
