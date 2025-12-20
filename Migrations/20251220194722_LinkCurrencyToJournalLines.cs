using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class LinkCurrencyToJournalLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_journal_lines_currency_id",
                table: "journal_lines",
                column: "currency_id");

            migrationBuilder.AddForeignKey(
                name: "FK_journal_lines_currencies_currency_id",
                table: "journal_lines",
                column: "currency_id",
                principalTable: "currencies",
                principalColumn: "currency_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_lines_currencies_currency_id",
                table: "journal_lines");

            migrationBuilder.DropIndex(
                name: "IX_journal_lines_currency_id",
                table: "journal_lines");
        }
    }
}
