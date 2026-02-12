using MediatR;

namespace Invoicer.Features.Products.CreateProduct
{
    public readonly record struct CreateProductCommand(
        Guid CompanyId,
        string Name,
        decimal Price,
        string Description,
        string ImageUrl
    ) : IRequest<CreateProductResponse>;
}
