using System.ComponentModel.DataAnnotations;
using MediatR;

namespace Invoicer.Features.Products.CreateProduct
{
    public readonly record struct CreateProductCommand(
        Guid CompanyId,
        [Required, StringLength(200)] string Name,
        [Range(0.01, 1_000_000_000)] decimal Price,
        [Required, StringLength(2000)] string Description,
        [Url, StringLength(2048)] string? ImageUrl
    ) : IRequest<CreateProductResponse>;
}
