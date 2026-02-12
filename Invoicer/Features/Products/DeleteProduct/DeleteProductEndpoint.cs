using MediatR;

namespace Invoicer.Features.Products.DeleteProduct
{
    public static class DeleteProductEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteProductEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "delete-product/{CompanyId}/{ProductId}",
                    async (Guid CompanyId, Guid ProductId, ISender sender) =>
                    {
                        var command = new DeleteProductCommand(CompanyId, ProductId);
                        await sender.Send(command);
                        return Results.NoContent();
                    }
                )
                .WithName("DeleteProduct")
                .RequireAuthorization();
            return app;
        }
    }
}
