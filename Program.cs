using EnterpriseERP.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection: Register AppDbContext with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in config.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Add Controllers with JSON serialization resilience
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// 3. Configure Enterprise CORS Policies
const string EnterpriseCorsPolicy = "EnterpriseCorsPolicy";

builder.Services.AddCors(options =>
{
    // Strict production policy with credentials for live Vercel domain & preview builds
    options.AddPolicy(EnterpriseCorsPolicy, policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            var uri = new Uri(origin);
            return uri.Host.Equals("enterprise-erp-system-wwwroot.vercel.app", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .WithExposedHeaders("Content-Disposition", "X-Pagination", "Total-Count");
    });

    // Permissive fallback policy for developer tooling & Swagger
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 4. Register Swagger Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Configure Middleware Pipeline
app.UseRouting();

// Enable Enterprise CORS (must be placed between UseRouting and UseAuthorization)
app.UseCors(EnterpriseCorsPolicy);

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise ERP API v1");
        c.RoutePrefix = "swagger";
    });
}

// Liveness / Readiness Health Check Endpoint for Cloud Hosting (Render, Fly.io, Railway)
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    framework = ".NET 9 / .NET 10 Web API",
    system = "Enterprise ERP Core Engine"
})).AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// 6. Database Auto-Migration and Seeding Scope
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate(); // Ensures tables are auto-created in newly provisioned databases
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult(); // Seed foundational enterprise data
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.Run();