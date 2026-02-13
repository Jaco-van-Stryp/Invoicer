using Invoicer.Infrastructure.StorageService;
using MediatR;

namespace Invoicer.Features.File.Download;

public class DownloadFileHandler(IStorageService storageService) : IRequestHandler<DownloadFileQuery, Stream>
{
    public Task<Stream> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        return storageService.DownloadFileAsync(request.Filename);
    }
}
