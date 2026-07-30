using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "sales_invoice_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "purchase_invoice_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PurchaseCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "numeric", nullable: false),
                    LowStockThreshold = table.Column<decimal>(type: "numeric", nullable: false),
                    IncomeAccountId = table.Column<int>(type: "integer", nullable: false),
                    ExpenseAccountId = table.Column<int>(type: "integer", nullable: false),
                    AssetAccountId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    InvoiceId = table.Column<int>(type: "integer", nullable: true),
                    BillId = table.Column<int>(type: "integer", nullable: true),
                    QuantityChange = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_InventoryItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoice_lines_ItemId",
                table: "sales_invoice_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoice_lines_ItemId",
                table: "purchase_invoice_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ItemId",
                table: "InventoryTransactions",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoice_lines_InventoryItems_ItemId",
                table: "purchase_invoice_lines",
                column: "ItemId",
                principalTable: "InventoryItems",
                principalColumn: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_sales_invoice_lines_InventoryItems_ItemId",
                table: "sales_invoice_lines",
                column: "ItemId",
                principalTable: "InventoryItems",
                principalColumn: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoice_lines_InventoryItems_ItemId",
                table: "purchase_invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_invoice_lines_InventoryItems_ItemId",
                table: "sales_invoice_lines");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoice_lines_ItemId",
                table: "sales_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoice_lines_ItemId",
                table: "purchase_invoice_lines");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "sales_invoice_lines");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "purchase_invoice_lines");
        }
    }
}
