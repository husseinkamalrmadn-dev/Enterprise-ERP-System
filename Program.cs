using EnterpriseERP.API.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection: Register AppDbContext with MS SQL Server (MonsterASP.NET compatible)
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
    // Production policy explicitly allowing the live Vercel frontend domain, subdomains, and localhost
    options.AddPolicy(EnterpriseCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "https://enterprise-erp-system-wwwroot.vercel.app"
            )
            .SetIsOriginAllowed(origin =>
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
// Forward headers for reverse proxy and IIS environments
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseRouting();

// Enable Enterprise CORS (must be placed between UseRouting and UseAuthorization / MapControllers)
app.UseCors(EnterpriseCorsPolicy);

// Enable Swagger in both Development and Production for live API verification at /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise ERP API v1");
    c.RoutePrefix = "swagger";
});

// Liveness / Readiness Health Check Endpoint for MonsterASP.NET & Vercel
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    framework = ".NET 9 / .NET 10 Web API",
    system = "Enterprise ERP Core Engine",
    hosting = "MonsterASP.NET (https://enterprise-erp-api.runasp.net)",
    database = "MS SQL Server 2025 (db74965.databaseasp.net)"
})).AllowAnonymous();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

app.UseAuthorization();

app.MapControllers().RequireCors(EnterpriseCorsPolicy);

// 6. Database Auto-Migration and Seeding Scope
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Seed foundational enterprise data if tables exist and are empty
        DbSeeder.SeedAsync(context).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Note: Seeding deferred or database initialized via migration.sql script.");
    }
}

app.Run();
