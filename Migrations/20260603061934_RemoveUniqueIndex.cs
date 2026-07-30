using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_journal_entries_number",
                table: "journal_entries");

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_number",
                table: "journal_entries",
                column: "journal_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_journal_entries_number",
                table: "journal_entries");

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_number",
                table: "journal_entries",
                column: "journal_number",
                unique: true);
        }
    }
}
