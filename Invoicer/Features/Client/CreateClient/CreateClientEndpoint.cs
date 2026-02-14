using System;
using MediatR;

namespace Invoicer.Features.Client.CreateClient;

public static class CreateClientEndpoint
{
    public static IEndpointRouteBuilder MapCreateClientEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "create-client",
                async (CreateClientCommand command, ISender mediator) =>
                {
                    var result = await mediator.Send(command);
                    return TypedResults.Ok(result);
                }
            )
            .WithName("CreateClient")
            .RequireAuthorization();

        return app;
    }
}
