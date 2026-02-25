using Invoicer.Features.Estimate.CreateEstimate;
using Invoicer.Features.Estimate.DeleteEstimate;
using Invoicer.Features.Estimate.GetAllEstimates;
using Invoicer.Features.Estimate.UpdateEstimate;

namespace Invoicer.Features.Estimate
{
    public static class EstimateEndpoints
    {
        public static IEndpointRouteBuilder MapEstimateEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/estimate").WithTags("Estimate");

            group.MapGetAllEstimatesEndpoint();
            group.MapCreateEstimateEndpoint();
            group.MapUpdateEstimateEndpoint();
            group.MapDeleteEstimateEndpoint();

            return app;
        }
    }
}
