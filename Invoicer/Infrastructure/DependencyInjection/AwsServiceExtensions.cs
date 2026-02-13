using Amazon.SimpleEmailV2;
using Invoicer.Infrastructure.EmailService;
using Invoicer.Infrastructure.EmailTemplateService;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class AwsServiceExtensions
{
    public static IServiceCollection AddAwsServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var sesOptions = configuration.GetSection("SES").Get<SesOptions>() ?? new SesOptions();

        services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ =>
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(sesOptions.Region);
            return new AmazonSimpleEmailServiceV2Client(region);
        });

        services.Configure<SesOptions>(configuration.GetSection("SES"));
        services.AddSingleton<IEmailService, EmailService.EmailService>();
        services.AddSingleton<IEmailTemplateService, EmailTemplateService.EmailTemplateService>();

        return services;
    }
}
