using Microsoft.OpenApi;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Invoicer API", Version = "v1" });

            opt.AddServer(
                new OpenApiServer { Url = "https://localhost:7261", Description = "Development HTTPS" }
            );
            opt.AddServer(
                new OpenApiServer { Url = "http://localhost:5244", Description = "Development HTTP" }
            );

            opt.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer",
                }
            );

            opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(),
            });
        });

        return services;
    }
}
