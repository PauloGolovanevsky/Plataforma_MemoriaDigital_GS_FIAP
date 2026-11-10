using MemoriaDigital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MemoriaDigital.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    public ApiFactory()
    {
        _connection.Open();
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddSingleton(_connection);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}
