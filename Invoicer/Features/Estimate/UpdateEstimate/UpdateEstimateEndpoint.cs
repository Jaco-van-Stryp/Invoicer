using MediatR;

namespace Invoicer.Features.Estimate.UpdateEstimate
{
    public static class UpdateEstimateEndpoint
    {
        public static IEndpointRouteBuilder MapUpdateEstimateEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPatch(
                    "update-estimate",
                    async (UpdateEstimateCommand command, ISender sender) =>
                    {
                        await sender.Send(command);
                        return TypedResults.Ok();
                    }
                )
                .WithName("UpdateEstimate")
                .RequireAuthorization();
            return app;
        }
    }
}
