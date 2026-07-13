using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using MyBackend.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers(); // Enable controllers

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

// Configure CORS to allow access from local and external clients
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

// Must be called early to correct request properties (scheme/host) based on proxy headers
app.UseForwardedHeaders();

// Serve static files from wwwroot (enables monolithic SPA hosting)
app.UseDefaultFiles();
app.UseStaticFiles();

// Automatically create database and tables on startup if they don't exist
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        
        // Safety check for production environments: ensure the new tables are created if the database already existed
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `Users` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `Username` LONGTEXT NOT NULL,
                `Email` VARCHAR(255) NOT NULL,
                `PasswordHash` LONGTEXT NOT NULL,
                `PasswordSalt` LONGTEXT NOT NULL,
                `CreatedAt` DATETIME NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");
        
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS `UserSessions` (
                `Id` INT AUTO_INCREMENT PRIMARY KEY,
                `UserId` INT NOT NULL,
                `Token` VARCHAR(255) NOT NULL,
                `ExpiresAt` DATETIME NOT NULL,
                `CreatedAt` DATETIME NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        ");

        app.Logger.LogInformation("Database initialization check passed (EnsureCreated + Table validation).");
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
    options.RoutePrefix = "swagger"; // Swagger UI will be at /swagger/index.html
});

// Enable CORS policy
app.UseCors("AllowAll");

// Map all Controllers
app.MapControllers();

app.Run();
