using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioBalance.Data;

namespace PortfolioBalance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly PortfolioDbContext _context;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(PortfolioDbContext context, ILogger<DatabaseController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Apply pending database migrations
    /// </summary>
    /// <returns>Information about the migration operation</returns>
    [HttpPost("migrate")]
    public async Task<ActionResult<MigrationResult>> ApplyMigrations()
    {
        try
        {
            var result = new MigrationResult();

            // Check if database can be connected to
            bool databaseExists = await _context.Database.CanConnectAsync();
            result.DatabaseExists = databaseExists;

            _logger.LogInformation("Migration endpoint called. Database exists: {DatabaseExists}", databaseExists);

            if (databaseExists)
            {
                // Get pending and applied migrations
                var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToList();
                var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync()).ToList();

                result.AppliedMigrations = appliedMigrations;
                result.PendingMigrationsBefore = pendingMigrations;

                _logger.LogInformation("Applied migrations: {Count}", appliedMigrations.Count);
                _logger.LogInformation("Pending migrations: {Count}", pendingMigrations.Count);

                // Check if database was created without migrations (EnsureCreated scenario)
                if (!appliedMigrations.Any() && !pendingMigrations.Any())
                {
                    _logger.LogWarning("Database exists but has no migration history and no pending migrations.");
                    _logger.LogWarning("This indicates the database was created without migrations.");

                    result.Message = "Database exists but has no migration history. Database will be recreated with proper migration tracking.";
                    result.Action = "Recreate database";

                    await _context.Database.EnsureDeletedAsync();
                    _logger.LogInformation("Database deleted. Applying migrations from scratch...");

                    await _context.Database.MigrateAsync();
                    result.MigrationsApplied = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
                    result.Success = true;

                    _logger.LogInformation("Database recreated and migrations applied successfully.");
                }
                else if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying pending migrations: {Migrations}", string.Join(", ", pendingMigrations));

                    result.Message = $"Applying {pendingMigrations.Count} pending migration(s).";
                    result.Action = "Apply migrations";

                    await _context.Database.MigrateAsync();
                    result.MigrationsApplied = pendingMigrations;
                    result.Success = true;

                    _logger.LogInformation("Migrations applied successfully.");
                }
                else
                {
                    result.Message = "Database is already up to date. No migrations needed.";
                    result.Action = "None";
                    result.Success = true;

                    _logger.LogInformation("Database is up to date.");
                }
            }
            else
            {
                _logger.LogInformation("Database does not exist. Creating with migrations...");

                result.Message = "Database does not exist. Creating with migrations.";
                result.Action = "Create database";

                await _context.Database.MigrateAsync();
                result.MigrationsApplied = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
                result.Success = true;

                _logger.LogInformation("Database created and migrations applied successfully.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while applying migrations");
            return StatusCode(500, new MigrationResult
            {
                Success = false,
                Message = $"Error applying migrations: {ex.Message}",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get migration status without applying any changes
    /// </summary>
    /// <returns>Current migration status</returns>
    [HttpGet("migration-status")]
    public async Task<ActionResult<MigrationStatusResult>> GetMigrationStatus()
    {
        try
        {
            var status = new MigrationStatusResult();

            bool databaseExists = await _context.Database.CanConnectAsync();
            status.DatabaseExists = databaseExists;

            if (databaseExists)
            {
                var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToList();
                var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync()).ToList();

                status.AppliedMigrations = appliedMigrations;
                status.PendingMigrations = pendingMigrations;
                status.IsUpToDate = !pendingMigrations.Any();
                status.NeedsMigration = pendingMigrations.Any() || (!appliedMigrations.Any() && !pendingMigrations.Any());
            }
            else
            {
                status.NeedsMigration = true;
                status.IsUpToDate = false;
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking migration status");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool DatabaseExists { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrationsBefore { get; set; } = new();
    public List<string> MigrationsApplied { get; set; } = new();
    public string? Error { get; set; }
}

public class MigrationStatusResult
{
    public bool DatabaseExists { get; set; }
    public bool IsUpToDate { get; set; }
    public bool NeedsMigration { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrations { get; set; } = new();
}
