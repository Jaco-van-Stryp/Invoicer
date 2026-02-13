using Invoicer.Infrastructure.StorageService;
using MediatR;

namespace Invoicer.Features.File.Upload;

public class UploadFileHandler(IStorageService storageService) : IRequestHandler<UploadFileCommand, string>
{
    public async Task<string> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        return await storageService.UploadFileAsync(request.FileStream);
    }
}
