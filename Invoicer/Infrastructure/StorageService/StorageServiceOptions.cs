namespace Invoicer.Infrastructure.StorageService;

public class StorageServiceOptions
{
    public string Endpoint { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string BucketName { get; set; } = "invoicer";
}
