using MapHive;
using MapHive.Middleware;
using MapHive.Repositories;
using MapHive.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add repository services
builder.Services.AddScoped<IMapLocationRepository, MapLocationRepository>();

// Add HTTP context accessor for accessing request information in services
builder.Services.AddHttpContextAccessor();

// Add session state services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add the LogManager service
builder.Services.AddScoped<LogManager>();

WebApplication app = builder.Build();

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

// Enable session before error handling middleware
app.UseSession();

// Add error handling middleware after session is configured
app.UseErrorHandling();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

MainClient.Initialize();

app.Run();