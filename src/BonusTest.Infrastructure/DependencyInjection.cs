using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BonusTest.Application.Bonuses;
using BonusTest.Application.Catalogs;
using BonusTest.Infrastructure.Persistence;
using BonusTest.Infrastructure.Repositories;

namespace BonusTest.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Se lee de la variable de entorno ConnectionStrings__BonusTest (no se versiona en appsettings.json).
        var connectionString = configuration.GetConnectionString("BonusTest");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'BonusTest'. Defina la variable de entorno ConnectionStrings__BonusTest.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IBonusRepository, BonusRepository>();

        return services;
    }
}
