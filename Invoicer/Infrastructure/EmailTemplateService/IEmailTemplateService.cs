namespace Invoicer.Infrastructure.EmailTemplateService
{
    public interface IEmailTemplateService
    {
        string RenderTemplate(
            EmailTemplateName templateName,
            Dictionary<string, string> placeholders
        );
    }
}
