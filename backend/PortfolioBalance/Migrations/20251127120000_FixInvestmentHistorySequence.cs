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
            // Reset the sequence for InvestmentHistories to the max ID + 1
            // This fixes the duplicate key constraint violation error
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
            // No rollback needed for sequence reset
        }
    }
}
