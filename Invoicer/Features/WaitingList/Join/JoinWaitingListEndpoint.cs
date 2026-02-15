using System;
using MediatR;

namespace Invoicer.Features.WaitingList.Join;

public static class JoinWaitingListEndpoint
{
    public static IEndpointRouteBuilder MapJoinWaitingListEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("join", async (JoinWaitingListCommand command, ISender sender) =>
        {
            await sender.Send(command);
            return TypedResults.Ok();
        });

        return app;
    }
}
