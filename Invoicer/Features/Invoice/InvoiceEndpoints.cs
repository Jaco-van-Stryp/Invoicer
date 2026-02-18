using Invoicer.Features.Invoice.CreateInvoice;
using Invoicer.Features.Invoice.DeleteInvoice;
using Invoicer.Features.Invoice.GetAllInvoices;
using Invoicer.Features.Invoice.GetDashboardStats;
using Invoicer.Features.Invoice.UpdateInvoice;

namespace Invoicer.Features.Invoice
{
    public static class InvoiceEndpoints
    {
        public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/invoice").WithTags("Invoice");

            group.MapCreateInvoiceEndpoint();
            group.MapGetAllInvoicesEndpoint();
            group.MapUpdateInvoiceEndpoint();
            group.MapDeleteInvoiceEndpoint();
            group.MapGetDashboardStatsEndpoint();

            return app;
        }
    }
}
