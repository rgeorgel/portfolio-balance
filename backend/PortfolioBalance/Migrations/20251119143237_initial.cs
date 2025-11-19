using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PortfolioBalance.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestmentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Investments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InvestmentTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Investments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Investments_InvestmentTypes_InvestmentTypeId",
                        column: x => x.InvestmentTypeId,
                        principalTable: "InvestmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Investments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInvestmentTypeAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InvestmentTypeId = table.Column<int>(type: "integer", nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInvestmentTypeAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInvestmentTypeAllocations_InvestmentTypes_InvestmentTy~",
                        column: x => x.InvestmentTypeId,
                        principalTable: "InvestmentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInvestmentTypeAllocations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "InvestmentTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Ações Nacionais" },
                    { 2, "Fundos Imobiliários" },
                    { 3, "Criptomoedas" },
                    { 4, "Renda Fixa" },
                    { 5, "Ações Internacionais" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Investments_InvestmentTypeId",
                table: "Investments",
                column: "InvestmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Investments_UserId",
                table: "Investments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvestmentTypeAllocations_InvestmentTypeId",
                table: "UserInvestmentTypeAllocations",
                column: "InvestmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvestmentTypeAllocations_UserId_InvestmentTypeId",
                table: "UserInvestmentTypeAllocations",
                columns: new[] { "UserId", "InvestmentTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Investments");

            migrationBuilder.DropTable(
                name: "UserInvestmentTypeAllocations");

            migrationBuilder.DropTable(
                name: "InvestmentTypes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
