namespace Invoicer.Infrastructure.EmailService
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(
            string toEmail,
            string subject,
            string bodyHtml,
            string? bodyText = null
        );
    }
}
