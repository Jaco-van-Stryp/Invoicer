using System;
using MediatR;

namespace Invoicer.Features.File.Upload;

public static class UploadFileEndpoint
{
    public static IEndpointRouteBuilder MapUploadFileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "upload",
                async (UploadFileCommand command, ISender sender) =>
                {
                    var result = await sender.Send(command);
                    return TypedResults.Ok(result);
                }
            )
            .WithName("UploadFile")
            .Accepts<UploadFileCommand>("multipart/form-data")
            .RequireAuthorization();
        return app;
    }
}
