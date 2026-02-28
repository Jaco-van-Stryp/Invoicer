using Invoicer.Features.File.Download;
using Invoicer.Features.File.Upload;

namespace Invoicer.Features.File;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/file").WithTags("File");

        group.MapUploadFileEndpoint();
        group.MapDownloadFileEndpoint();

        return app;
    }
}
