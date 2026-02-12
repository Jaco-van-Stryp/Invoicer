namespace Invoicer.Features.Products.CreateProduct
{
    public record struct CreateProductResponse(
        Guid ProductId,
        string Name,
        decimal Price,
        string Description,
        string ImageUrl,
        Guid CompanyId
    );
}
