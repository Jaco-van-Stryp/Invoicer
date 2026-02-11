using MediatR;

namespace Invoicer.Features.Company.CreateCompany
{
    public static class CreateCompanyEndpoint
    {
        public static IEndpointRouteBuilder MapCreateCompanyEndpoint(this IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "CreateCompany",
                    async (CreateCompanyCommand command, ISender sender) =>
                    {
                        var result = await sender.Send(command);
                        return TypedResults.Ok(result);
                    }
                )
                .WithName("CreateCompany");
            return app;
        }
    }
}
