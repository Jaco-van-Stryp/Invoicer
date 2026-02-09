namespace Invoicer.Infrastructure.EmailService
{
    public class SesOptions
    {
        public string Region { get; set; } = "ap-southeast-2"; // e.g. Sydney for NZ
        public string FromEmail { get; set; } = "no-reply@invoicer.co.nz"; // must be from verified domain
        public string FromName { get; set; } = "Invoicer";
    }
}
