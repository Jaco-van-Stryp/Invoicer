using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class SwaggerServiceExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        services.AddEndpointsApiExplorer();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Invoicer API", Version = "v1" });

            if (environment.IsDevelopment())
            {
                opt.AddServer(new OpenApiServer
                {
                    Url = "https://localhost:7261",
                    Description = "Development HTTPS",
                });
                opt.AddServer(new OpenApiServer
                {
                    Url = "http://localhost:5244",
                    Description = "Development HTTP",
                });
            }
            else
            {
                var serverUrls = configuration.GetSection("Swagger:ServerUrls").Get<string[]>() ?? [];
                foreach (var url in serverUrls)
                {
                    opt.AddServer(new OpenApiServer { Url = url });
                }
            }

            opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer",
            });

            opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
            });
        });

        return services;
    }
}