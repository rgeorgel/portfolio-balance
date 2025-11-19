using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioBalance.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitValueAndQuantityToInvestments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitValue",
                table: "Investments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Investments",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitValue",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Investments");
        }
    }
}
