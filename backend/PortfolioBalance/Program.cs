using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortfolioBalance.Data;
using PortfolioBalance.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Database Context
builder.Services.AddDbContext<PortfolioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Services
builder.Services.AddScoped<PortfolioBalancingService>();
builder.Services.AddScoped<AuthService>();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Apply database migrations (if enabled in configuration)
var autoMigrateOnStartup = builder.Configuration.GetValue<bool>("Database:AutoMigrateOnStartup", true);

if (autoMigrateOnStartup)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<PortfolioDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Automatic migration on startup is ENABLED");

            bool databaseExists = context.Database.CanConnect();

            logger.LogInformation($"Database exists: {databaseExists}");

            if (databaseExists)
            {
                // Check for pending and applied migrations
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                var appliedMigrations = context.Database.GetAppliedMigrations().ToList();

                logger.LogInformation($"Applied migrations: {appliedMigrations.Count}");
                logger.LogInformation($"Pending migrations: {pendingMigrations.Count}");

                // If the database exists but has no migration history, it was likely created
                // with EnsureCreated() which doesn't use migrations. We need to recreate it.
                if (!appliedMigrations.Any() && !pendingMigrations.Any())
                {
                    logger.LogWarning("Database exists but has no migration history and no pending migrations.");
                    logger.LogWarning("This indicates the database was created without migrations (e.g., using EnsureCreated).");
                    logger.LogWarning("Deleting database to recreate it with proper migration tracking...");
                    context.Database.EnsureDeleted();
                    logger.LogInformation("Database deleted. Will apply migrations from scratch...");
                    databaseExists = false;
                }
                else if (pendingMigrations.Any())
                {
                    logger.LogInformation("Pending migrations found: {Migrations}", string.Join(", ", pendingMigrations));
                    context.Database.Migrate();
                    logger.LogInformation("Database migrations applied successfully.");
                }
                else
                {
                    logger.LogInformation("Database is up to date. No migrations needed.");
                }
            }

            // If database doesn't exist (or was just deleted), create it with migrations
            if (!databaseExists)
            {
                logger.LogInformation("Creating database with migrations...");
                context.Database.Migrate();
                logger.LogInformation("Database created and migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
            throw;
        }
    }
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("Automatic migration on startup is DISABLED. Use /api/database/migrate endpoint to apply migrations manually.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
