namespace Invoicer.Features.Products.UpdateProduct
{
    public readonly record struct UpdateProductCommand(
        Guid CompanyId,
        Guid ProductId,
        string? Name,
        decimal? Price,
        string? Description,
        string? ImageUrl
    ) : MediatR.IRequest;
}
