using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DigitalTwin.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DigitalTwinDbContext>
{
    public DigitalTwinDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "../DigitalTwin.Api");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DigitalTwinDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("Postgres"));

        return new DigitalTwinDbContext(optionsBuilder.Options);
    }
}