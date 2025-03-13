using MapHive;
using MapHive.Repositories;
using MapHive.Services;
using MapHive.Middleware;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add repository services
builder.Services.AddScoped<IMapLocationRepository, MapLocationRepository>();

// Add HTTP context accessor for accessing request information in services
builder.Services.AddHttpContextAccessor();

// Add the LogManager service
builder.Services.AddScoped<LogManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Add error handling middleware first to catch all exceptions
app.UseErrorHandling();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

MainClient.Initialize();

app.Run(); 