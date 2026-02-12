using MediatR;

namespace Invoicer.Features.Products.CreateProduct
{
    public static class CreateProductEndpoint
    {
        public static IEndpointRouteBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "create-product",
                    async (CreateProductCommand command, ISender sender) =>
                    {
                        return TypedResults.Ok(await sender.Send(command));
                    }
                )
                .WithName("CreateProduct");
            return app;
        }
    }
}
