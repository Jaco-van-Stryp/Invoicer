using Invoicer.Features.Products.CreateProduct;
using Invoicer.Features.Products.GetAllProducts;
using Invoicer.Features.Products.UpdateProduct;

namespace Invoicer.Features.Products
{
    public static class ProductsEndpoints
    {
        public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/product").WithTags("Product");

            group.MapCreateProductEndpoint();
            group.MapGetAllProductsEndpoint();
            group.MapUpdateProductEndpoint();

            return app;
        }
    }
}
