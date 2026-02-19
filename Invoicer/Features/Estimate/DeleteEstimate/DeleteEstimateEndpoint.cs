using MediatR;

namespace Invoicer.Features.Estimate.DeleteEstimate
{
    public static class DeleteEstimateEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteEstimateEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapDelete(
                    "delete-estimate",
                    async (Guid estimateId, ISender sender) =>
                    {
                        var command = new DeleteEstimateCommand(estimateId);
                        await sender.Send(command);
                        return TypedResults.NoContent();
                    }
                )
                .WithName("DeleteEstimate")
                .RequireAuthorization();

            return app;
        }
    }
}
