using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using Serilog;

namespace Invoicer.Infrastructure.EmailService
{
    public class EmailService : IEmailService
    {
        private readonly IAmazonSimpleEmailServiceV2 _sesClient;
        private readonly SesOptions _options;

        public EmailService(IAmazonSimpleEmailServiceV2 sesClient, IOptions<SesOptions> options)
        {
            _sesClient = sesClient;
            _options = options.Value;
        }

        public async Task<bool> SendEmailAsync(
            string toEmail,
            string subject,
            string bodyHtml,
            string? bodyText = null
        )
        {
            var fromAddress = $"{_options.FromName} <{_options.FromEmail}>";

            var content = new Body
            {
                Html = new Content { Data = bodyHtml, Charset = "UTF-8" },
            };

            if (!string.IsNullOrEmpty(bodyText))
            {
                content.Text = new Content { Data = bodyText, Charset = "UTF-8" };
            }

            var message = new Message
            {
                Subject = new Content { Data = subject, Charset = "UTF-8" },
                Body = content,
            };

            var destination = new Destination { ToAddresses = new List<string> { toEmail } };

            var request = new SendEmailRequest
            {
                FromEmailAddress = fromAddress,
                Destination = destination,
                Content = new EmailContent { Simple = message },
            };

            try
            {
                var response = await _sesClient.SendEmailAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to send email");
                throw new EmailException();
            }
        }
    }
}
