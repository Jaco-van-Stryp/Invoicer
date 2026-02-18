using Invoicer.Features.Auth;
using Invoicer.Features.Client;
using Invoicer.Features.Company;
using Invoicer.Features.File;
using Invoicer.Features.Invoice;
using Invoicer.Features.Payment;
using Invoicer.Features.Products;
using Invoicer.Features.WaitingList;

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
        group.MapPaymentEndpoints();
        group.MapWaitingListEndpoints();

        return app;
    }
}
