using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioBalance.Migrations
{
    /// <inheritdoc />
    public partial class FixInvestmentHistorySequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reset the sequence to the next available value
            // This fixes the duplicate key issue by ensuring the sequence is synced with existing data
            migrationBuilder.Sql(@"
                SELECT setval(
                    pg_get_serial_sequence('""InvestmentHistories""', 'Id'),
                    COALESCE((SELECT MAX(""Id"") FROM ""InvestmentHistories""), 0) + 1,
                    false
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No action needed for rollback
        }
    }
}
