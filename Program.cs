using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DCAS.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

// Turn on legacy timestamp handling to keep PostgreSQL from complaining about local DateTime formats
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure the database connection dynamically based on the environment
builder.Services.AddDbContext<DCASContext>(options =>
{
    // Check if the application is running on Render by looking for the custom Postgres environment variable
    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DCASContext");
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        // PRODUCTION (Render): Use PostgreSQL database driver
        options.UseNpgsql(connectionString);
    }
    else
    {
        // LOCAL DEVELOPMENT: Retrieve local connection string from appsettings.json and use SQL Server driver
        connectionString = builder.Configuration.GetConnectionString("DCASContext");
        options.UseSqlServer(connectionString ?? throw new InvalidOperationException("Connection string 'DCASContext' not found."));
    }
});

// Add cookie authentication services to the container
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication middleware setup (must execute before Authorization)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Automatically apply any missing EF Core database migrations at application startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DCASContext>();
        context.Database.Migrate(); 
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
