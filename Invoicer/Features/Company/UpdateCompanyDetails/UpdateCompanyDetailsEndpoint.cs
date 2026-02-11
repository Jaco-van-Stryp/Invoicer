using MediatR;

namespace Invoicer.Features.Company.UpdateCompanyDetails
{
    public static class UpdateCompanyDetailsEndpoint
    {
        public static IEndpointRouteBuilder MapUpdateCompanyDetailsEndpoint(
            this IEndpointRouteBuilder app
        )
        {
            app.MapPatch(
                    "update-company-details",
                    async (UpdateCompanyDetailsCommand command, ISender sender) =>
                    {
                        await sender.Send(command);
                        return TypedResults.Ok();
                    }
                )
                .WithName("UpdateCompanyDetails")
                .RequireAuthorization();
            return app;
        }
    }
}
