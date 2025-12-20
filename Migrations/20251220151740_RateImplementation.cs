using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class RateImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "currency_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "sales_invoices",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "currency_id",
                table: "purchase_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "purchase_invoices",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "ContactId",
                table: "journal_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactType",
                table: "journal_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "currency_id",
                table: "journal_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "journal_lines",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "foreign_credit_amount",
                table: "journal_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "foreign_debit_amount",
                table: "journal_lines",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "currency_id",
                table: "journal_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "exchange_rate",
                table: "journal_entries",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<DateTime>(
                name: "date_of_birth",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "customers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "grade_level",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guardian_email",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guardian_name",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guardian_phone",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "national_id",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nationality",
                table: "customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "student_id",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "currency_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "currency_id",
                table: "purchase_invoices");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "purchase_invoices");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "ContactType",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "currency_id",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "foreign_credit_amount",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "foreign_debit_amount",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "currency_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "exchange_rate",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "date_of_birth",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "grade_level",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "guardian_email",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "guardian_name",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "guardian_phone",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "national_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "nationality",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "student_id",
                table: "customers");
        }
    }
}
