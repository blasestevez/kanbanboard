using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trellochocero.Api.Data;

namespace Trellochocero.Tests.Integration;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"TestDb-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Database=test_dummy" }
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d =>
                {
                    var typeName = d.ServiceType.FullName ?? string.Empty;
                    var implName = d.ImplementationType?.FullName ?? string.Empty;
                    return typeName.Contains("DbContextOptions") ||
                           typeName.Contains("Npgsql") ||
                           implName.Contains("Npgsql");
                })
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }
}
