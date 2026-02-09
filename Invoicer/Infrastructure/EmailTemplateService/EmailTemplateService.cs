using System.Collections.Concurrent;
using System.Reflection;

namespace Invoicer.Infrastructure.EmailTemplateService
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly Assembly _assembly = typeof(EmailTemplateService).Assembly;
        private readonly string _resourcePrefix = "Invoicer.Infrastructure.EmailTemplateService.Templates";
        private readonly ConcurrentDictionary<string, string> _cache = new();

        public string RenderTemplate(EmailTemplateName templateName, Dictionary<string, string> placeholders)
        {
            var body = LoadTemplate(templateName.ToString());
            var layout = LoadTemplate("_Layout");

            body = ReplacePlaceholders(body, placeholders);

            var html = layout
                .Replace("{{Body}}", body)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());

            return html;
        }

        private string LoadTemplate(string name)
        {
            return _cache.GetOrAdd(name, key =>
            {
                var resourceName = $"{_resourcePrefix}.{key}.html";
                using var stream = _assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Email template '{key}' not found as embedded resource '{resourceName}'.");
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            });
        }

        private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
        {
            foreach (var (key, value) in placeholders)
            {
                template = template.Replace($"{{{{{key}}}}}", value);
            }

            return template;
        }
    }
}
