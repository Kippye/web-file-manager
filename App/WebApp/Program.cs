using Application;
using Application.Contracts;
using Domain.Identity;
using Infrastructure.Contracts;
using Infrastructure.EF;
using Infrastructure.EF.OperationProcessing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
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

// NOTE: This currently does everything that AddDefaultIdentity does
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme; // TODO: Why is this ExternalScheme?
})
.AddIdentityCookies();
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Stores.MaxLengthForKeys = 128;
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddDefaultUI()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AppDbContext>();
// Configure cookies - shorter lifespan
builder.Services.ConfigureApplicationCookie(options =>
{
    // No "Remember me" -> Session expiration
    // With "Remember me" -> Expire in 7 days, instead of default 14
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    // options.Cookie.SameSite = SameSiteMode.Strict; // Should this be set to Strict instead of default Lax?
    // options.SlidingExpiration // Whether to extend the lifetime of valid cookies as they are used
});

builder.Services.AddScoped<LoggingAntiforgeryFilter>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<LoggingAntiforgeryFilter>();
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IFileValidator, FileValidator>();
builder.Services.AddSingleton<IFileEncryptionService, FileEncryptionService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Set up database and / or data based on configuration
app.UseDataInitialization(app.Configuration);

// Configure the HTTP request pipeline.
// NOTE: HTTP is (temporarily?) allowed in Development
// in Production, all HTTP gets upgraded to HTTPS
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

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
