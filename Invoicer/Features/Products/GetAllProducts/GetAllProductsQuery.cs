using MediatR;

namespace Invoicer.Features.Products.GetProducts
{
    public readonly record struct GetAllProductsQuery(Guid CompanyId)
        : IRequest<List<GetAllProductsResponse>>;
}
