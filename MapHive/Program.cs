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
builder.Services.AddSingleton<IMapLocationRepository, MapLocationRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddSingleton<IReviewRepository, ReviewRepository>();
builder.Services.AddSingleton<IDiscussionRepository, DiscussionRepository>();
builder.Services.AddSingleton<IDataGridRepository, DataGridRepository>();
builder.Services.AddSingleton<IDisplayPageRepository, DisplayPageRepository>();
builder.Services.AddSingleton<ILogRepository, LogRepository>();

// Add application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IDataGridService, DataGridService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddSingleton<IDatabaseUpdaterSingleton, DatabaseUpdaterService>();

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
builder.Services.AddSingleton<IConfigurationSingleton, ConfigurationSingleton>();

// Register FileLoggerService as Singleton
builder.Services.AddSingleton<IFileLoggerSingleton, FileLoggerSingleton>();

builder.Services.AddSingleton<ILogManagerSingleton, LogManagerSingleton>();

// Register IDisplayPageService
builder.Services.AddScoped<IDisplayPageService, DisplayPageService>();

// Register application services for MVC controllers
builder.Services.AddScoped<IMapService, MapService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IRequestContextService, RequestContextService>();
builder.Services.AddScoped<IUserFriendlyExceptionService, UserFriendlyExceptionService>();

// Register DatabaseUpdaterService as Singleton

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Run the DatabaseUpdaterService to apply any pending updates
IDatabaseUpdaterSingleton dbUpdaterService = app.Services.GetService<IDatabaseUpdaterSingleton>() ?? throw new Exception($"{nameof(IDatabaseUpdaterSingleton)} not found!");
await dbUpdaterService.RunAsync();

// Use the injected ConfigService to check DevelopmentMode
if (!await (app.Services.GetService<IConfigurationSingleton>() ?? throw new Exception($"{nameof(IConfigurationSingleton)} not found!")).GetDevelopmentModeAsync())
{
    _ = app.UseExceptionHandler(errorHandlingPath: "/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.Run();
{ }
