using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.Cookies;
using MyBackend.Data;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers(); // Maintain API controllers if required
builder.Services.AddRazorPages();  // Register Razor Pages monolithic support

// Register Cookie Authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/"; // Our Index Razor Page serves as the login screen
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Configure Forwarded Headers to respect reverse proxy headers (like Traefik in Dokploy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure MySQL database connection using Pomelo EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30))));

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Respect proxy headers
app.UseForwardedHeaders();

// Serve static assets from wwwroot (like app.css)
app.UseStaticFiles();

// Automatically create database and tables on startup if they don't exist
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        app.Logger.LogInformation("Database initialization check passed (EnsureCreated).");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initializing the database. Ensure MySQL is running and your connection string in appsettings.json is correct.");
    }
}

// Configure Swagger API documentation routing
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "My API v1");
    options.RoutePrefix = "swagger";
});

// Enable CORS policy
app.UseCors("AllowAll");

// Monolithic session protection middlewares
app.UseAuthentication();
app.UseAuthorization();

// Map routing endpoints
app.MapControllers();
app.MapRazorPages(); // Direct matching for *.cshtml routes

app.Run();
