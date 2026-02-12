namespace Invoicer.Features.Products.GetAllProducts
{
    public record struct GetAllProductsResponse(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        string ImageUrl
    );
}
