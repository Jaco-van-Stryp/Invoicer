using Invoicer.Features.Products.CreateProduct;

namespace Invoicer.Features.Products
{
    public static class ProductsEndpoints
    {
        public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/product").WithTags("Product");

            group.MapCreateProductEndpoint();

            return app;
        }
    }
}
