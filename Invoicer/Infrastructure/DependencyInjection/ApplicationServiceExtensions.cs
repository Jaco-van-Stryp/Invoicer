using Invoicer.Infrastructure.ExceptionHandling;
using Invoicer.Infrastructure.Validation;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAll",
                policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });

        return services;
    }
}
