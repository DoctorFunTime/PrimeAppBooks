using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class CreateBankReconTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_cleared",
                table: "journal_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "reconciliation_id",
                table: "journal_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bank_reconciliations",
                columns: table => new
                {
                    reconciliation_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    statement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    statement_starting_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    statement_ending_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cleared_difference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    created_by = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_reconciliations", x => x.reconciliation_id);
                    table.ForeignKey(
                        name: "FK_bank_reconciliations_chart_of_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_reconciliation_id",
                table: "journal_lines",
                column: "reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_reconciliations_account_id",
                table: "bank_reconciliations",
                column: "account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_journal_lines_bank_reconciliations_reconciliation_id",
                table: "journal_lines",
                column: "reconciliation_id",
                principalTable: "bank_reconciliations",
                principalColumn: "reconciliation_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_lines_bank_reconciliations_reconciliation_id",
                table: "journal_lines");

            migrationBuilder.DropTable(
                name: "bank_reconciliations");

            migrationBuilder.DropIndex(
                name: "IX_journal_lines_reconciliation_id",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "is_cleared",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "reconciliation_id",
                table: "journal_lines");
        }
    }
}
