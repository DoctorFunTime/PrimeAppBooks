using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class RefinedInvoiceManagementColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "auto_invoice_frequency",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "auto_invoice_interval",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "is_auto_invoice_enabled",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_auto_invoice_date",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auto_invoice_frequency",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "auto_invoice_interval",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_auto_invoice_enabled",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "next_auto_invoice_date",
                table: "customers");
        }
    }
}
