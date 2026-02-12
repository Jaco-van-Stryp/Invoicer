namespace Invoicer.Features.Products.DeleteProduct
{
    public readonly record struct DeleteProductCommand(Guid CompanyId, Guid ProductId)
        : MediatR.IRequest;
}
