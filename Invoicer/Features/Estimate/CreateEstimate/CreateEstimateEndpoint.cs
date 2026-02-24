using MediatR;

namespace Invoicer.Features.Estimate.CreateEstimate
{
    public static class CreateEstimateEndpoint
    {
        public static IEndpointRouteBuilder MapCreateEstimateEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapPost(
                    "create-estimate",
                    async (CreateEstimateCommand command, ISender sender) =>
                    {
                        var result = await sender.Send(command);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("CreateEstimate")
                .RequireAuthorization();

            return app;
        }
    }
}
