namespace Invoicer.Features.Products.GetProducts
{
    public record struct GetAllProductsResponse(
        Guid Id,
        string Name,
        string Description,
        decimal Price,
        string ImageUrl
    );
}
