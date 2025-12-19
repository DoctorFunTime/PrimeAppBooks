using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedInvoiceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "auto_invoice_amount",
                table: "customers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auto_invoice_amount",
                table: "customers");
        }
    }
}
