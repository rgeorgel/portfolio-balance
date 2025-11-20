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
                // IMPORTANT: We do NOT delete the database to avoid data loss!
                if (!appliedMigrations.Any() && !pendingMigrations.Any())
                {
                    _logger.LogError("CRITICAL: Database exists but has no migration history and no pending migrations.");
                    _logger.LogError("This indicates the database was created without migrations.");
                    _logger.LogError("To prevent data loss, the database will NOT be automatically deleted.");

                    result.Message = "Database exists without migration history. To prevent data loss, automatic deletion is disabled. Please manually backup and delete the database if needed.";
                    result.Action = "Manual intervention required";
                    result.Success = false;
                    result.Error = "Database exists without migration history. Manual intervention required to prevent data loss.";

                    return StatusCode(409, result); // 409 Conflict - indicates action required
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

    /// <summary>
    /// Fix database sequences that may be out of sync
    /// This resolves duplicate key errors by resetting sequences to the next available value
    /// </summary>
    /// <returns>Result of the sequence fix operation</returns>
    [HttpPost("fix-sequences")]
    public async Task<ActionResult<SequenceFixResult>> FixSequences()
    {
        try
        {
            var result = new SequenceFixResult();
            var sequencesFixed = new List<string>();

            _logger.LogInformation("Fixing database sequences...");

            // Fix InvestmentHistories sequence
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT setval(
                    pg_get_serial_sequence('""InvestmentHistories""', 'Id'),
                    COALESCE((SELECT MAX(""Id"") FROM ""InvestmentHistories""), 0) + 1,
                    false
                );
            ");
            sequencesFixed.Add("InvestmentHistories");

            // Fix Investments sequence
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT setval(
                    pg_get_serial_sequence('""Investments""', 'Id'),
                    COALESCE((SELECT MAX(""Id"") FROM ""Investments""), 0) + 1,
                    false
                );
            ");
            sequencesFixed.Add("Investments");

            // Fix Users sequence
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT setval(
                    pg_get_serial_sequence('""Users""', 'Id'),
                    COALESCE((SELECT MAX(""Id"") FROM ""Users""), 0) + 1,
                    false
                );
            ");
            sequencesFixed.Add("Users");

            // Fix UserInvestmentTypeAllocations sequence
            await _context.Database.ExecuteSqlRawAsync(@"
                SELECT setval(
                    pg_get_serial_sequence('""UserInvestmentTypeAllocations""', 'Id'),
                    COALESCE((SELECT MAX(""Id"") FROM ""UserInvestmentTypeAllocations""), 0) + 1,
                    false
                );
            ");
            sequencesFixed.Add("UserInvestmentTypeAllocations");

            result.Success = true;
            result.Message = $"Successfully fixed {sequencesFixed.Count} sequences";
            result.SequencesFixed = sequencesFixed;

            _logger.LogInformation("Sequences fixed successfully: {Sequences}", string.Join(", ", sequencesFixed));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fixing sequences");
            return StatusCode(500, new SequenceFixResult
            {
                Success = false,
                Message = $"Error fixing sequences: {ex.Message}",
                Error = ex.Message
            });
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

public class SequenceFixResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> SequencesFixed { get; set; } = new();
    public string? Error { get; set; }
}
