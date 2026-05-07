using DigitalTwin.Application.Abstractions.Caching;
using DigitalTwin.Application.Abstractions.External;
using DigitalTwin.Infrastructure.Caching;
using DigitalTwin.Infrastructure.External.BambuProxy;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DigitalTwin.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DigitalTwinDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        var useRedis = configuration.GetValue<bool>("Redis:Enabled");

        if (useRedis)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"]!));

            services.AddScoped<IFleetCache, RedisFleetCache>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<IFleetCache, MemoryFleetCache>();
        }

        services.AddHttpClient<IBambuProxyClient, BambuProxyClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["BambuProxy:BaseUrl"]!);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    configuration["BambuProxy:ApiToken"]);
        });

        return services;
    }
}