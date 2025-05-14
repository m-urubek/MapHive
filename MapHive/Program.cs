using MapHive.Middleware;
using MapHive.Repositories;
using MapHive.Services;
using MapHive.Singletons;
using Microsoft.AspNetCore.Authentication.Cookies;
using reCAPTCHA.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(assemblies: typeof(MapHive.Models.BusinessModels.MappingProfile).Assembly);

// Register HttpContextAccessor first
builder.Services.AddHttpContextAccessor();

// Add repository services
builder.Services.AddScoped<IMapLocationRepository, MapLocationRepository>();
builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IDiscussionRepository, DiscussionRepository>();
builder.Services.AddScoped<IDataGridRepository, DataGridRepository>();
builder.Services.AddScoped<IDisplayPageRepository, DisplayPageRepository>();
builder.Services.AddScoped<IIpBansRepository, IpBansRepository>();
builder.Services.AddScoped<IAccountBansRepository, AccountBansRepository>();
builder.Services.AddSingleton<ILogRepository, LogRepository>(); //LogRepository cannot be scoped because it cannot log to avoid circular reference

// Add application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IDataGridService, DataGridService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IIpBansService, IpBansService>();
builder.Services.AddScoped<IAccountBansService, AccountBansService>();

builder.Services.AddSingleton<IDatabaseUpdaterSingleton, DatabaseUpdaterSingleton>();

// Add SqlClient as a singleton
builder.Services.AddSingleton<ISqlClientSingleton, SqlClientSingleton>();

builder.Services.AddScoped<ILogManagerService, LogManagerService>();

// Add reCAPTCHA service
builder.Services.Configure<RecaptchaSettings>(config: builder.Configuration.GetSection(key: "RecaptchaSettings"));
builder.Services.AddTransient<RecaptchaService>();
builder.Services.AddTransient<IRecaptchaService, RecaptchaService>();

// Add authentication
builder.Services.AddAuthentication(defaultScheme: CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(configureOptions: options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(value: 7);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add session state services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(configure: options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(value: 30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register ConfigService as Singleton (responsible for loading/caching DB settings)
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// Register FileLoggerService as Singleton
builder.Services.AddSingleton<IFileLoggerSingleton, FileLoggerSingleton>();

builder.Services.AddSingleton<ILogManagerSingleton, LogManagerSingleton>();

// Register IDisplayPageService
builder.Services.AddScoped<IDisplayPageService, DisplayPageService>();

// Register application services for MVC controllers
builder.Services.AddScoped<IMapService, MapLocationService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IRequestContextService, RequestContextService>();
builder.Services.AddScoped<IUserFriendlyExceptionService, UserFriendlyExceptionService>();

// Register DatabaseUpdaterSingleton as Singleton

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Add custom route for /Logs to AdminController.Logs
app.MapControllerRoute(
    name: "logs_short",
    pattern: "Logs",
    defaults: new { controller = "Admin", action = "Logs" }
);

// Default conventional route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the DatabaseUpdaterSingleton to apply any pending updates
IDatabaseUpdaterSingleton dbUpdaterService = app.Services.GetService<IDatabaseUpdaterSingleton>() ?? throw new Exception($"{nameof(IDatabaseUpdaterSingleton)} not found!");
await dbUpdaterService.RunAsync();

// Use the injected ConfigService to check DevelopmentMode
using (IServiceScope scope = app.Services.CreateScope())
{
    IConfigurationService configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
    if (!await configService.GetDevelopmentModeAsync())
    {
        _ = app.UseExceptionHandler(errorHandlingPath: "/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        _ = app.UseHsts();
    }
}

app.Run();
{ }
