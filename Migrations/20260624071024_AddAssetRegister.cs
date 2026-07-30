using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SalePrice",
                table: "InventoryItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchaseCost",
                table: "InventoryItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.CreateTable(
                name: "asset_categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    default_useful_life_years = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    default_depreciation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "STRAIGHT_LINE"),
                    default_asset_account_id = table.Column<int>(type: "integer", nullable: true),
                    default_accum_depn_account_id = table.Column<int>(type: "integer", nullable: true),
                    default_depn_expense_account_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "fixed_assets",
                columns: table => new
                {
                    asset_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    asset_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category_id = table.Column<int>(type: "integer", nullable: false),
                    asset_account_id = table.Column<int>(type: "integer", nullable: false),
                    accum_depn_account_id = table.Column<int>(type: "integer", nullable: false),
                    depn_expense_account_id = table.Column<int>(type: "integer", nullable: false),
                    purchase_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    purchase_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    residual_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    useful_life_years = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    depreciation_method = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "STRAIGHT_LINE"),
                    accumulated_depreciation = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    book_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false, defaultValue: "ACTIVE"),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fixed_assets", x => x.asset_id);
                    table.ForeignKey(
                        name: "FK_fixed_assets_asset_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "asset_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fixed_assets_chart_of_accounts_accum_depn_account_id",
                        column: x => x.accum_depn_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fixed_assets_chart_of_accounts_asset_account_id",
                        column: x => x.asset_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fixed_assets_chart_of_accounts_depn_expense_account_id",
                        column: x => x.depn_expense_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_disposals",
                columns: table => new
                {
                    disposal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    disposal_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sale_proceeds = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    book_value_at_disposal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    disposal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "SALE"),
                    proceeds_account_id = table.Column<int>(type: "integer", nullable: true),
                    journal_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_disposals", x => x.disposal_id);
                    table.ForeignKey(
                        name: "FK_asset_disposals_fixed_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "fixed_assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_asset_disposals_journal_entries_journal_id",
                        column: x => x.journal_id,
                        principalTable: "journal_entries",
                        principalColumn: "journal_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "depreciation_entries",
                columns: table => new
                {
                    entry_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    period_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    depreciation_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    book_value_after = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    journal_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_depreciation_entries", x => x.entry_id);
                    table.ForeignKey(
                        name: "FK_depreciation_entries_fixed_assets_asset_id",
                        column: x => x.asset_id,
                        principalTable: "fixed_assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_depreciation_entries_journal_entries_journal_id",
                        column: x => x.journal_id,
                        principalTable: "journal_entries",
                        principalColumn: "journal_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_disposals_asset_id",
                table: "asset_disposals",
                column: "asset_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asset_disposals_journal_id",
                table: "asset_disposals",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_depreciation_entries_asset_id",
                table: "depreciation_entries",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_depreciation_entries_journal_id",
                table: "depreciation_entries",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_accum_depn_account_id",
                table: "fixed_assets",
                column: "accum_depn_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_asset_account_id",
                table: "fixed_assets",
                column: "asset_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_asset_code",
                table: "fixed_assets",
                column: "asset_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_category_id",
                table: "fixed_assets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_depn_expense_account_id",
                table: "fixed_assets",
                column: "depn_expense_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_disposals");

            migrationBuilder.DropTable(
                name: "depreciation_entries");

            migrationBuilder.DropTable(
                name: "fixed_assets");

            migrationBuilder.DropTable(
                name: "asset_categories");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalePrice",
                table: "InventoryItems",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "PurchaseCost",
                table: "InventoryItems",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
