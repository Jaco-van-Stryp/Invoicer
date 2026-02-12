using Invoicer.Features.Products.CreateProduct;
using Invoicer.Features.Products.GetAllProducts;

namespace Invoicer.Features.Products
{
    public static class ProductsEndpoints
    {
        public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/product").WithTags("Product");

            group.MapCreateProductEndpoint();
            group.MapGetAllProductsEndpoint();
            return app;
        }
    }
}
