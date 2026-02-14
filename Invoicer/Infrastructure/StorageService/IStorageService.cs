namespace Invoicer.Infrastructure.StorageService;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream);
    Task<Stream> DownloadFileAsync(Guid fileName);
}
