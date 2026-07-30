using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchasesSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoice_lines_InventoryItems_ItemId",
                table: "purchase_invoice_lines");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "purchase_invoice_lines",
                newName: "item_id");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_invoice_lines_ItemId",
                table: "purchase_invoice_lines",
                newName: "IX_purchase_invoice_lines_item_id");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoice_lines_InventoryItems_item_id",
                table: "purchase_invoice_lines",
                column: "item_id",
                principalTable: "InventoryItems",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoice_lines_InventoryItems_item_id",
                table: "purchase_invoice_lines");

            migrationBuilder.RenameColumn(
                name: "item_id",
                table: "purchase_invoice_lines",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_invoice_lines_item_id",
                table: "purchase_invoice_lines",
                newName: "IX_purchase_invoice_lines_ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoice_lines_InventoryItems_ItemId",
                table: "purchase_invoice_lines",
                column: "ItemId",
                principalTable: "InventoryItems",
                principalColumn: "ItemId");
        }
    }
}
