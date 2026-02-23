using MediatR;

namespace Invoicer.Features.Estimate.DeleteEstimate
{
    public static class DeleteEstimateEndpoint
    {
        public static IEndpointRouteBuilder MapDeleteEstimateEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapDelete(
                    "delete-estimate/{EstimateId}",
                    async (Guid EstimateId, ISender sender) =>
                    {
                        var command = new DeleteEstimateCommand(EstimateId);
                        await sender.Send(command);
                        return Results.NoContent();
                    }
                )
                .WithName("DeleteEstimate")
                .RequireAuthorization();

            return app;
        }
    }
}
