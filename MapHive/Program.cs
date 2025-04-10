using MapHive.Middleware;
using MapHive.Repositories;
using MapHive.Repositories.Interfaces;
using MapHive.Services;
using MapHive.Singletons;
using MapHive.Utilities;
using Microsoft.AspNetCore.Authentication.Cookies;
using reCAPTCHA.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add repository services
builder.Services.AddScoped<IMapLocationRepository, MapLocationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IDiscussionRepository, DiscussionRepository>();
builder.Services.AddScoped<IDataGridRepository, DataGridRepository>();
builder.Services.AddScoped<IDisplayRepository, DisplayRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// Add SqlClient as a singleton
builder.Services.AddSingleton(sp => {
    string dbFilePath = "D:\\MapHive\\MapHive\\maphive.db";
    if (!File.Exists(dbFilePath))
    {
        dbFilePath = "maphive.db";
    }
    return MapHive.SqlClient.GetInstance(dbFilePath);
});

// Add HTTP context accessor for accessing request information in services
builder.Services.AddHttpContextAccessor();

// Add reCAPTCHA service
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("RecaptchaSettings"));
builder.Services.AddTransient<RecaptchaService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add session state services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add the LogManagerService service
builder.Services.AddScoped<LogManagerService>();

// Add the AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Add the ConfigService
builder.Services.AddScoped<IConfigService, ConfigService>();

WebApplication app = builder.Build();

// Initialize the CurrentRequest static class with the service provider
CurrentRequest.Initialize(app.Services);

// Initialize the main client
MainClient.Initialize();

// Update the database with any new tables or columns
using (IServiceScope serviceScope = app.Services.CreateScope())
{
    DatabaseManipulator databaseUpdater = new();
    databaseUpdater.UpdateDatabase();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    _ = app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    _ = app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseErrorHandling();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();