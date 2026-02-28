using Invoicer.Infrastructure.StorageService;
using Microsoft.AspNetCore.Http;

namespace Invoicer.Features.File.Upload;

public static class UploadFileEndpoint
{
    public static IEndpointRouteBuilder MapUploadFileEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
                "upload",
                async (IFormFile fileStream, IStorageService storageService) =>
                {
                    if (!fileStream.ContentType.StartsWith("image/"))
                        return Results.Problem(
                            "Only image files are allowed.",
                            statusCode: StatusCodes.Status400BadRequest
                        );

                    using var stream = fileStream.OpenReadStream();
                    var result = await storageService.UploadFileAsync(stream);
                    return Results.Ok(result);
                }
            )
            .WithName("UploadFile")
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .RequireAuthorization();
        return app;
    }
}
