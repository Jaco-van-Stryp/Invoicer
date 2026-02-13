using Invoicer.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Invoicer.Infrastructure.DependencyInjection;

public static class DataServiceExtensions
{
    public static IServiceCollection AddPostgres(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        return services;
    }
}
