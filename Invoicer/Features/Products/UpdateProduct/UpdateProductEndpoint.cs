using MediatR;

namespace Invoicer.Features.Products.UpdateProduct
{
    public static class UpdateProductEndpoint
    {
        public static IEndpointRouteBuilder MapUpdateProductEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPatch(
                    "update-product",
                    async (UpdateProductCommand command, ISender sender) =>
                    {
                        await sender.Send(command);
                        return TypedResults.Ok();
                    }
                )
                .WithName("UpdateProduct")
                .RequireAuthorization();
            return app;
        }
    }
}
