using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetCWIPStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cwip_account_id",
                table: "fixed_assets",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fixed_assets_cwip_account_id",
                table: "fixed_assets",
                column: "cwip_account_id");

            migrationBuilder.AddForeignKey(
                name: "FK_fixed_assets_chart_of_accounts_cwip_account_id",
                table: "fixed_assets",
                column: "cwip_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fixed_assets_chart_of_accounts_cwip_account_id",
                table: "fixed_assets");

            migrationBuilder.DropIndex(
                name: "IX_fixed_assets_cwip_account_id",
                table: "fixed_assets");

            migrationBuilder.DropColumn(
                name: "cwip_account_id",
                table: "fixed_assets");
        }
    }
}
