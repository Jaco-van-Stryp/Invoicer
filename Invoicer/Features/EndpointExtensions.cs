using Invoicer.Features.Auth;
using Invoicer.Features.Client;
using Invoicer.Features.Company;
using Invoicer.Features.File;
using Invoicer.Features.Invoice;
using Invoicer.Features.Products;

namespace Invoicer.Features;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("API");

        group.MapAuthEndpoints();
        group.MapCompanyEndpoints();
        group.MapProductEndpoints();
        group.MapFileEndpoints();
        group.MapClientEndpoints();
        group.MapInvoiceEndpoints();

        return app;
    }
}
