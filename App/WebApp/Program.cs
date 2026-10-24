using Application;
using Application.Contracts;
using Domain.Identity;
using Infrastructure.Contracts;
using Infrastructure.EF;
using Infrastructure.EF.OperationProcessing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using WebApp;
using WebApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<LogEnricher>();
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration) // Read base settings from app configuration
    .Enrich.With(services.GetRequiredService<LogEnricher>())
    .Destructure.ByTransforming<EventId>(e => new { e.Id, e.Name }) // Avoid adding $type
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/log.json") // TODO: Configure log directory externally?
    .WriteTo.Seq(ctx.Configuration.GetRequiredSection("Seq").GetValue<string>("ServerUrl")!) // Write to Seq for UI log viewing
);
builder.Services.AddSingleton(typeof(IAppLogger<>), typeof(AppLogger<>));

// NOTE: IUserResolverService is used by entity action strategies
builder.Services.AddSingleton<IUserResolverService, UserResolverService>();
builder.Services.AddSingleton<IEntityActionStrategy, AuditEntityCreatedStrategy>();
builder.Services.AddSingleton<IEntityActionStrategy, AuditEntityUpdatedStrategy>();
builder.Services.AddSingleton<IDataOperationProcessor, DataOperationProcessor>();

builder.Services.AddDataProtection()
    .SetApplicationName("FileMan")
    .PersistKeysToDbContext<AppDbContext>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IFileValidator, FileValidator>();
builder.Services.AddSingleton<IFileEncryptionService, FileEncryptionService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

// TODO: Move
using var serviceScope = app.Services
     .GetRequiredService<IServiceScopeFactory>()
     .CreateScope();

using var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();
var fileStorageService = serviceScope.ServiceProvider.GetRequiredService<IFileStorageService>();

if (app.Configuration.GetValue<bool>("DataInitialization:ClearFiles"))
{
    Log.Warning("ClearFiles");
    fileStorageService.DeleteAllFilesAsync().Wait();
}

if (app.Configuration.GetValue<bool>("DataInitialization:DropDatabase"))
{
    Log.Warning("DropDatabase");
    context.Database.EnsureDeleted();
}

// NOTE: Only for testing (bypasses migrations to create full schema)
if (app.Configuration.GetValue<bool>("DataInitialization:CreateDatabase"))
{
    Log.Information("CreateDatabase");
    context.Database.EnsureCreated();
}

if (app.Configuration.GetValue<bool>("DataInitialization:MigrateDatabase"))
{
    Log.Information("MigrateDatabase");
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id:guid?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// Custom default response headers middleware
app.UseResponseHeaders();

app.Run();
