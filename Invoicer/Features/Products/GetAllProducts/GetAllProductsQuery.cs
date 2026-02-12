using MediatR;

namespace Invoicer.Features.Products.GetAllProducts
{
    public readonly record struct GetAllProductsQuery(Guid CompanyId)
        : IRequest<List<GetAllProductsResponse>>;
}
