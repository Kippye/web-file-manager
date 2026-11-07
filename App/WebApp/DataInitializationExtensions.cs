using Application.Contracts;
using Infrastructure.EF;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace WebApp;

public static class DataInitializationExtensions
{
    public static IApplicationBuilder UseDataInitialization(this IApplicationBuilder appBuilder, IConfiguration configuration)
    {
        using var serviceScope = appBuilder.ApplicationServices
            .GetRequiredService<IServiceScopeFactory>()
            .CreateScope();

        using var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fileStorageService = serviceScope.ServiceProvider.GetRequiredService<IFileStorageService>();

        if (configuration.GetValue<bool>("DataInitialization:ClearFiles"))
        {
            Log.Warning("ClearFiles");
            fileStorageService.DeleteAllFilesAsync().Wait();
        }

        if (configuration.GetValue<bool>("DataInitialization:DropDatabase"))
        {
            Log.Warning("DropDatabase");
            context.Database.EnsureDeleted();
        }

        // NOTE: Only for testing (bypasses migrations to create full schema)
        if (configuration.GetValue<bool>("DataInitialization:CreateDatabase"))
        {
            Log.Information("CreateDatabase");
            context.Database.EnsureCreated();
        }

        if (configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
        {
            Log.Information("MigrateDatabase");
            context.Database.Migrate();
        }
        return appBuilder;
    }
}
