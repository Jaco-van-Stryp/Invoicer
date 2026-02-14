using Invoicer.Infrastructure.ExceptionHandling;
using Invoicer.Infrastructure.Validation;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
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
                policy =>
                {
                    if (environment.IsDevelopment())
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }
                    else
                    {
                        var origins =
                            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                        var methods =
                            configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
                            ?? ["GET", "POST", "PUT", "DELETE"];
                        var headers =
                            configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
                            ?? ["Authorization", "Content-Type"];

                        policy.WithOrigins(origins).WithMethods(methods).WithHeaders(headers);
                    }
                }
            );
        });

        return services;
    }
}
