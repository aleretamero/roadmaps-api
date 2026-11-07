using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roadmaps.Infrastructure.Persistence;

namespace Roadmaps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not configured.");

        services.AddDbContext<RoadmapsDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
