using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddedInvoicesEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bills_Vendors_vendor_id",
                table: "bills");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_Vendors_vendor_id",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoiceLines_PurchaseInvoices_PurchaseInvoiceId",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoiceLines_chart_of_accounts_AccountId",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseInvoices_Vendors_VendorId",
                table: "PurchaseInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoiceLines_chart_of_accounts_AccountId",
                table: "SalesInvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Customers_CustomerId",
                table: "SalesInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendors",
                table: "Vendors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxRates",
                table: "TaxRates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesInvoices",
                table: "SalesInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesInvoiceLines",
                table: "SalesInvoiceLines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseInvoices",
                table: "PurchaseInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseInvoiceLines",
                table: "PurchaseInvoiceLines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountingSettings",
                table: "AccountingSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccountingPeriods",
                table: "AccountingPeriods");

            migrationBuilder.RenameTable(
                name: "Vendors",
                newName: "vendors");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "customers");

            migrationBuilder.RenameTable(
                name: "Currencies",
                newName: "currencies");

            migrationBuilder.RenameTable(
                name: "TaxRates",
                newName: "tax_rates");

            migrationBuilder.RenameTable(
                name: "SalesInvoices",
                newName: "sales_invoices");

            migrationBuilder.RenameTable(
                name: "SalesInvoiceLines",
                newName: "sales_invoice_lines");

            migrationBuilder.RenameTable(
                name: "PurchaseInvoices",
                newName: "purchase_invoices");

            migrationBuilder.RenameTable(
                name: "PurchaseInvoiceLines",
                newName: "purchase_invoice_lines");

            migrationBuilder.RenameTable(
                name: "PaymentMethods",
                newName: "payment_methods");

            migrationBuilder.RenameTable(
                name: "AccountingSettings",
                newName: "accounting_settings");

            migrationBuilder.RenameTable(
                name: "AccountingPeriods",
                newName: "accounting_periods");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "vendors",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "vendors",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "vendors",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "vendors",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "VendorName",
                table: "vendors",
                newName: "vendor_name");

            migrationBuilder.RenameColumn(
                name: "VendorCode",
                table: "vendors",
                newName: "vendor_code");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "vendors",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TaxId",
                table: "vendors",
                newName: "tax_id");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "vendors",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DefaultPaymentTermsId",
                table: "vendors",
                newName: "default_payment_terms_id");

            migrationBuilder.RenameColumn(
                name: "DefaultExpenseAccountId",
                table: "vendors",
                newName: "default_expense_account_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "vendors",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContactPerson",
                table: "vendors",
                newName: "contact_person");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "vendors",
                newName: "vendor_id");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "customers",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "customers",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "customers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "customers",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TaxId",
                table: "customers",
                newName: "tax_id");

            migrationBuilder.RenameColumn(
                name: "ShippingAddress",
                table: "customers",
                newName: "shipping_address");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "customers",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "DefaultRevenueAccountId",
                table: "customers",
                newName: "default_revenue_account_id");

            migrationBuilder.RenameColumn(
                name: "DefaultPaymentTermsId",
                table: "customers",
                newName: "default_payment_terms_id");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "customers",
                newName: "customer_name");

            migrationBuilder.RenameColumn(
                name: "CustomerCode",
                table: "customers",
                newName: "customer_code");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "customers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContactPerson",
                table: "customers",
                newName: "contact_person");

            migrationBuilder.RenameColumn(
                name: "BillingAddress",
                table: "customers",
                newName: "billing_address");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "customers",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "currencies",
                newName: "symbol");

            migrationBuilder.RenameColumn(
                name: "IsBaseCurrency",
                table: "currencies",
                newName: "is_base_currency");

            migrationBuilder.RenameColumn(
                name: "CurrencyName",
                table: "currencies",
                newName: "currency_name");

            migrationBuilder.RenameColumn(
                name: "CurrencyCode",
                table: "currencies",
                newName: "currency_code");

            migrationBuilder.RenameColumn(
                name: "CurrencyId",
                table: "currencies",
                newName: "currency_id");

            migrationBuilder.RenameColumn(
                name: "Rate",
                table: "tax_rates",
                newName: "rate");

            migrationBuilder.RenameColumn(
                name: "TaxType",
                table: "tax_rates",
                newName: "tax_type");

            migrationBuilder.RenameColumn(
                name: "TaxName",
                table: "tax_rates",
                newName: "tax_name");

            migrationBuilder.RenameColumn(
                name: "TaxCode",
                table: "tax_rates",
                newName: "tax_code");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "tax_rates",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "EffectiveFrom",
                table: "tax_rates",
                newName: "effective_from");

            migrationBuilder.RenameColumn(
                name: "TaxRateId",
                table: "tax_rates",
                newName: "tax_rate_id");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "sales_invoices",
                newName: "terms");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "sales_invoices",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "sales_invoices",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "sales_invoices",
                newName: "balance");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "sales_invoices",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "sales_invoices",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "TaxAmount",
                table: "sales_invoices",
                newName: "tax_amount");

            migrationBuilder.RenameColumn(
                name: "NetAmount",
                table: "sales_invoices",
                newName: "net_amount");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "sales_invoices",
                newName: "invoice_number");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "sales_invoices",
                newName: "invoice_date");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "sales_invoices",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "sales_invoices",
                newName: "discount_amount");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "sales_invoices",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "sales_invoices",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sales_invoices",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AmountReceived",
                table: "sales_invoices",
                newName: "amount_received");

            migrationBuilder.RenameColumn(
                name: "SalesInvoiceId",
                table: "sales_invoices",
                newName: "sales_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoices_CustomerId",
                table: "sales_invoices",
                newName: "IX_sales_invoices_customer_id");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "sales_invoice_lines",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "sales_invoice_lines",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "sales_invoice_lines",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "sales_invoice_lines",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "SalesInvoiceId",
                table: "sales_invoice_lines",
                newName: "sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "sales_invoice_lines",
                newName: "account_id");

            migrationBuilder.RenameColumn(
                name: "LineId",
                table: "sales_invoice_lines",
                newName: "line_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoiceLines_SalesInvoiceId",
                table: "sales_invoice_lines",
                newName: "IX_sales_invoice_lines_sales_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoiceLines_AccountId",
                table: "sales_invoice_lines",
                newName: "IX_sales_invoice_lines_account_id");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "purchase_invoices",
                newName: "terms");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "purchase_invoices",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "purchase_invoices",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "purchase_invoices",
                newName: "balance");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "purchase_invoices",
                newName: "vendor_id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "purchase_invoices",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "purchase_invoices",
                newName: "total_amount");

            migrationBuilder.RenameColumn(
                name: "TaxAmount",
                table: "purchase_invoices",
                newName: "tax_amount");

            migrationBuilder.RenameColumn(
                name: "NetAmount",
                table: "purchase_invoices",
                newName: "net_amount");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "purchase_invoices",
                newName: "invoice_number");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "purchase_invoices",
                newName: "invoice_date");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "purchase_invoices",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "purchase_invoices",
                newName: "discount_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "purchase_invoices",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "purchase_invoices",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "purchase_invoices",
                newName: "amount_paid");

            migrationBuilder.RenameColumn(
                name: "PurchaseInvoiceId",
                table: "purchase_invoices",
                newName: "purchase_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseInvoices_VendorId",
                table: "purchase_invoices",
                newName: "IX_purchase_invoices_vendor_id");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "purchase_invoice_lines",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "purchase_invoice_lines",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "purchase_invoice_lines",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "purchase_invoice_lines",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "PurchaseInvoiceId",
                table: "purchase_invoice_lines",
                newName: "purchase_invoice_id");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "purchase_invoice_lines",
                newName: "account_id");

            migrationBuilder.RenameColumn(
                name: "LineId",
                table: "purchase_invoice_lines",
                newName: "line_id");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseInvoiceLines_PurchaseInvoiceId",
                table: "purchase_invoice_lines",
                newName: "IX_purchase_invoice_lines_purchase_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseInvoiceLines_AccountId",
                table: "purchase_invoice_lines",
                newName: "IX_purchase_invoice_lines_account_id");

            migrationBuilder.RenameColumn(
                name: "MethodName",
                table: "payment_methods",
                newName: "method_name");

            migrationBuilder.RenameColumn(
                name: "MethodCode",
                table: "payment_methods",
                newName: "method_code");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "payment_methods",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "MethodId",
                table: "payment_methods",
                newName: "method_id");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "accounting_settings",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "accounting_settings",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "SettingValue",
                table: "accounting_settings",
                newName: "setting_value");

            migrationBuilder.RenameColumn(
                name: "SettingKey",
                table: "accounting_settings",
                newName: "setting_key");

            migrationBuilder.RenameColumn(
                name: "SettingId",
                table: "accounting_settings",
                newName: "setting_id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "accounting_periods",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "PeriodNumber",
                table: "accounting_periods",
                newName: "period_number");

            migrationBuilder.RenameColumn(
                name: "PeriodName",
                table: "accounting_periods",
                newName: "period_name");

            migrationBuilder.RenameColumn(
                name: "IsClosed",
                table: "accounting_periods",
                newName: "is_closed");

            migrationBuilder.RenameColumn(
                name: "FiscalYear",
                table: "accounting_periods",
                newName: "fiscal_year");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "accounting_periods",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "ClosedAt",
                table: "accounting_periods",
                newName: "closed_at");

            migrationBuilder.RenameColumn(
                name: "PeriodId",
                table: "accounting_periods",
                newName: "period_id");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "vendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "vendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "vendor_name",
                table: "vendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "vendors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "tax_id",
                table: "vendors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "vendors",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "vendors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "contact_person",
                table: "vendors",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "tax_id",
                table: "customers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "customer_name",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "customers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "contact_person",
                table: "customers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "rate",
                table: "tax_rates",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "tax_rates",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "sales_invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DRAFT",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "balance",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "sales_invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "net_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "sales_invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_received",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "sales_invoice_lines",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "sales_invoice_lines",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "sales_invoice_lines",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "purchase_invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DRAFT",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "balance",
                table: "purchase_invoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "purchase_invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_amount",
                table: "purchase_invoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "purchase_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "net_amount",
                table: "purchase_invoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "discount_amount",
                table: "purchase_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "purchase_invoices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_paid",
                table: "purchase_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "purchase_invoice_lines",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount",
                table: "purchase_invoice_lines",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                table: "purchase_invoice_lines",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                table: "payment_methods",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "accounting_settings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "is_closed",
                table: "accounting_periods",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_vendors",
                table: "vendors",
                column: "vendor_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_customers",
                table: "customers",
                column: "customer_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_currencies",
                table: "currencies",
                column: "currency_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tax_rates",
                table: "tax_rates",
                column: "tax_rate_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sales_invoices",
                table: "sales_invoices",
                column: "sales_invoice_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sales_invoice_lines",
                table: "sales_invoice_lines",
                column: "line_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_purchase_invoices",
                table: "purchase_invoices",
                column: "purchase_invoice_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_purchase_invoice_lines",
                table: "purchase_invoice_lines",
                column: "line_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payment_methods",
                table: "payment_methods",
                column: "method_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_accounting_settings",
                table: "accounting_settings",
                column: "setting_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_accounting_periods",
                table: "accounting_periods",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_invoice_date",
                table: "sales_invoices",
                column: "invoice_date");

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_invoice_number",
                table: "sales_invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_invoices_status",
                table: "sales_invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_invoice_date",
                table: "purchase_invoices",
                column: "invoice_date");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_invoice_number",
                table: "purchase_invoices",
                column: "invoice_number");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_invoices_status",
                table: "purchase_invoices",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_bills_vendors_vendor_id",
                table: "bills",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "vendor_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_vendors_vendor_id",
                table: "payments",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "vendor_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoice_lines_chart_of_accounts_account_id",
                table: "purchase_invoice_lines",
                column: "account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoice_lines_purchase_invoices_purchase_invoice_id",
                table: "purchase_invoice_lines",
                column: "purchase_invoice_id",
                principalTable: "purchase_invoices",
                principalColumn: "purchase_invoice_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_purchase_invoices_vendors_vendor_id",
                table: "purchase_invoices",
                column: "vendor_id",
                principalTable: "vendors",
                principalColumn: "vendor_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_invoice_lines_chart_of_accounts_account_id",
                table: "sales_invoice_lines",
                column: "account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_invoice_lines_sales_invoices_sales_invoice_id",
                table: "sales_invoice_lines",
                column: "sales_invoice_id",
                principalTable: "sales_invoices",
                principalColumn: "sales_invoice_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sales_invoices_customers_customer_id",
                table: "sales_invoices",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "customer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bills_vendors_vendor_id",
                table: "bills");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_vendors_vendor_id",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoice_lines_chart_of_accounts_account_id",
                table: "purchase_invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoice_lines_purchase_invoices_purchase_invoice_id",
                table: "purchase_invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_purchase_invoices_vendors_vendor_id",
                table: "purchase_invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_invoice_lines_chart_of_accounts_account_id",
                table: "sales_invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_invoice_lines_sales_invoices_sales_invoice_id",
                table: "sales_invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_sales_invoices_customers_customer_id",
                table: "sales_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_vendors",
                table: "vendors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_customers",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_currencies",
                table: "currencies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tax_rates",
                table: "tax_rates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sales_invoices",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_invoice_date",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_invoice_number",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "IX_sales_invoices_status",
                table: "sales_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sales_invoice_lines",
                table: "sales_invoice_lines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_purchase_invoices",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoices_invoice_date",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoices_invoice_number",
                table: "purchase_invoices");

            migrationBuilder.DropIndex(
                name: "IX_purchase_invoices_status",
                table: "purchase_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_purchase_invoice_lines",
                table: "purchase_invoice_lines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payment_methods",
                table: "payment_methods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_accounting_settings",
                table: "accounting_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_accounting_periods",
                table: "accounting_periods");

            migrationBuilder.RenameTable(
                name: "vendors",
                newName: "Vendors");

            migrationBuilder.RenameTable(
                name: "customers",
                newName: "Customers");

            migrationBuilder.RenameTable(
                name: "currencies",
                newName: "Currencies");

            migrationBuilder.RenameTable(
                name: "tax_rates",
                newName: "TaxRates");

            migrationBuilder.RenameTable(
                name: "sales_invoices",
                newName: "SalesInvoices");

            migrationBuilder.RenameTable(
                name: "sales_invoice_lines",
                newName: "SalesInvoiceLines");

            migrationBuilder.RenameTable(
                name: "purchase_invoices",
                newName: "PurchaseInvoices");

            migrationBuilder.RenameTable(
                name: "purchase_invoice_lines",
                newName: "PurchaseInvoiceLines");

            migrationBuilder.RenameTable(
                name: "payment_methods",
                newName: "PaymentMethods");

            migrationBuilder.RenameTable(
                name: "accounting_settings",
                newName: "AccountingSettings");

            migrationBuilder.RenameTable(
                name: "accounting_periods",
                newName: "AccountingPeriods");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "Vendors",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Vendors",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Vendors",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Vendors",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "vendor_name",
                table: "Vendors",
                newName: "VendorName");

            migrationBuilder.RenameColumn(
                name: "vendor_code",
                table: "Vendors",
                newName: "VendorCode");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Vendors",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tax_id",
                table: "Vendors",
                newName: "TaxId");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Vendors",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "default_payment_terms_id",
                table: "Vendors",
                newName: "DefaultPaymentTermsId");

            migrationBuilder.RenameColumn(
                name: "default_expense_account_id",
                table: "Vendors",
                newName: "DefaultExpenseAccountId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Vendors",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "contact_person",
                table: "Vendors",
                newName: "ContactPerson");

            migrationBuilder.RenameColumn(
                name: "vendor_id",
                table: "Vendors",
                newName: "VendorId");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "Customers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Customers",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Customers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Customers",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "tax_id",
                table: "Customers",
                newName: "TaxId");

            migrationBuilder.RenameColumn(
                name: "shipping_address",
                table: "Customers",
                newName: "ShippingAddress");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Customers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "default_revenue_account_id",
                table: "Customers",
                newName: "DefaultRevenueAccountId");

            migrationBuilder.RenameColumn(
                name: "default_payment_terms_id",
                table: "Customers",
                newName: "DefaultPaymentTermsId");

            migrationBuilder.RenameColumn(
                name: "customer_name",
                table: "Customers",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "customer_code",
                table: "Customers",
                newName: "CustomerCode");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Customers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "contact_person",
                table: "Customers",
                newName: "ContactPerson");

            migrationBuilder.RenameColumn(
                name: "billing_address",
                table: "Customers",
                newName: "BillingAddress");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "Customers",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "symbol",
                table: "Currencies",
                newName: "Symbol");

            migrationBuilder.RenameColumn(
                name: "is_base_currency",
                table: "Currencies",
                newName: "IsBaseCurrency");

            migrationBuilder.RenameColumn(
                name: "currency_name",
                table: "Currencies",
                newName: "CurrencyName");

            migrationBuilder.RenameColumn(
                name: "currency_code",
                table: "Currencies",
                newName: "CurrencyCode");

            migrationBuilder.RenameColumn(
                name: "currency_id",
                table: "Currencies",
                newName: "CurrencyId");

            migrationBuilder.RenameColumn(
                name: "rate",
                table: "TaxRates",
                newName: "Rate");

            migrationBuilder.RenameColumn(
                name: "tax_type",
                table: "TaxRates",
                newName: "TaxType");

            migrationBuilder.RenameColumn(
                name: "tax_name",
                table: "TaxRates",
                newName: "TaxName");

            migrationBuilder.RenameColumn(
                name: "tax_code",
                table: "TaxRates",
                newName: "TaxCode");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "TaxRates",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "effective_from",
                table: "TaxRates",
                newName: "EffectiveFrom");

            migrationBuilder.RenameColumn(
                name: "tax_rate_id",
                table: "TaxRates",
                newName: "TaxRateId");

            migrationBuilder.RenameColumn(
                name: "terms",
                table: "SalesInvoices",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "SalesInvoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "SalesInvoices",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "balance",
                table: "SalesInvoices",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "SalesInvoices",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "SalesInvoices",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "tax_amount",
                table: "SalesInvoices",
                newName: "TaxAmount");

            migrationBuilder.RenameColumn(
                name: "net_amount",
                table: "SalesInvoices",
                newName: "NetAmount");

            migrationBuilder.RenameColumn(
                name: "invoice_number",
                table: "SalesInvoices",
                newName: "InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "invoice_date",
                table: "SalesInvoices",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "SalesInvoices",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "discount_amount",
                table: "SalesInvoices",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "SalesInvoices",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "SalesInvoices",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "SalesInvoices",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "amount_received",
                table: "SalesInvoices",
                newName: "AmountReceived");

            migrationBuilder.RenameColumn(
                name: "sales_invoice_id",
                table: "SalesInvoices",
                newName: "SalesInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_sales_invoices_customer_id",
                table: "SalesInvoices",
                newName: "IX_SalesInvoices_CustomerId");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "SalesInvoiceLines",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "SalesInvoiceLines",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "SalesInvoiceLines",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "SalesInvoiceLines",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "sales_invoice_id",
                table: "SalesInvoiceLines",
                newName: "SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "account_id",
                table: "SalesInvoiceLines",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "line_id",
                table: "SalesInvoiceLines",
                newName: "LineId");

            migrationBuilder.RenameIndex(
                name: "IX_sales_invoice_lines_sales_invoice_id",
                table: "SalesInvoiceLines",
                newName: "IX_SalesInvoiceLines_SalesInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_sales_invoice_lines_account_id",
                table: "SalesInvoiceLines",
                newName: "IX_SalesInvoiceLines_AccountId");

            migrationBuilder.RenameColumn(
                name: "terms",
                table: "PurchaseInvoices",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "PurchaseInvoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "PurchaseInvoices",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "balance",
                table: "PurchaseInvoices",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "vendor_id",
                table: "PurchaseInvoices",
                newName: "VendorId");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "PurchaseInvoices",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "total_amount",
                table: "PurchaseInvoices",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "tax_amount",
                table: "PurchaseInvoices",
                newName: "TaxAmount");

            migrationBuilder.RenameColumn(
                name: "net_amount",
                table: "PurchaseInvoices",
                newName: "NetAmount");

            migrationBuilder.RenameColumn(
                name: "invoice_number",
                table: "PurchaseInvoices",
                newName: "InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "invoice_date",
                table: "PurchaseInvoices",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "PurchaseInvoices",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "discount_amount",
                table: "PurchaseInvoices",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "PurchaseInvoices",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "PurchaseInvoices",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "amount_paid",
                table: "PurchaseInvoices",
                newName: "AmountPaid");

            migrationBuilder.RenameColumn(
                name: "purchase_invoice_id",
                table: "PurchaseInvoices",
                newName: "PurchaseInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_invoices_vendor_id",
                table: "PurchaseInvoices",
                newName: "IX_PurchaseInvoices_VendorId");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "PurchaseInvoiceLines",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "PurchaseInvoiceLines",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "PurchaseInvoiceLines",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "PurchaseInvoiceLines",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "purchase_invoice_id",
                table: "PurchaseInvoiceLines",
                newName: "PurchaseInvoiceId");

            migrationBuilder.RenameColumn(
                name: "account_id",
                table: "PurchaseInvoiceLines",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "line_id",
                table: "PurchaseInvoiceLines",
                newName: "LineId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_invoice_lines_purchase_invoice_id",
                table: "PurchaseInvoiceLines",
                newName: "IX_PurchaseInvoiceLines_PurchaseInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_invoice_lines_account_id",
                table: "PurchaseInvoiceLines",
                newName: "IX_PurchaseInvoiceLines_AccountId");

            migrationBuilder.RenameColumn(
                name: "method_name",
                table: "PaymentMethods",
                newName: "MethodName");

            migrationBuilder.RenameColumn(
                name: "method_code",
                table: "PaymentMethods",
                newName: "MethodCode");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "PaymentMethods",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "method_id",
                table: "PaymentMethods",
                newName: "MethodId");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "AccountingSettings",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "AccountingSettings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "setting_value",
                table: "AccountingSettings",
                newName: "SettingValue");

            migrationBuilder.RenameColumn(
                name: "setting_key",
                table: "AccountingSettings",
                newName: "SettingKey");

            migrationBuilder.RenameColumn(
                name: "setting_id",
                table: "AccountingSettings",
                newName: "SettingId");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "AccountingPeriods",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "period_number",
                table: "AccountingPeriods",
                newName: "PeriodNumber");

            migrationBuilder.RenameColumn(
                name: "period_name",
                table: "AccountingPeriods",
                newName: "PeriodName");

            migrationBuilder.RenameColumn(
                name: "is_closed",
                table: "AccountingPeriods",
                newName: "IsClosed");

            migrationBuilder.RenameColumn(
                name: "fiscal_year",
                table: "AccountingPeriods",
                newName: "FiscalYear");

            migrationBuilder.RenameColumn(
                name: "end_date",
                table: "AccountingPeriods",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "closed_at",
                table: "AccountingPeriods",
                newName: "ClosedAt");

            migrationBuilder.RenameColumn(
                name: "period_id",
                table: "AccountingPeriods",
                newName: "PeriodId");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "VendorName",
                table: "Vendors",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vendors",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "TaxId",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Vendors",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Vendors",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "ContactPerson",
                table: "Vendors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "TaxId",
                table: "Customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Customers",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "CustomerName",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "ContactPerson",
                table: "Customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "TaxRates",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "TaxRates",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SalesInvoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "DRAFT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "SalesInvoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetAmount",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SalesInvoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountReceived",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SalesInvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "SalesInvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "SalesInvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "PurchaseInvoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "DRAFT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "PurchaseInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "PurchaseInvoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "PurchaseInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "PurchaseInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetAmount",
                table: "PurchaseInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountAmount",
                table: "PurchaseInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "PurchaseInvoices",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountPaid",
                table: "PurchaseInvoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "PurchaseInvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "PurchaseInvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "PurchaseInvoiceLines",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "PaymentMethods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "AccountingSettings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<bool>(
                name: "IsClosed",
                table: "AccountingPeriods",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendors",
                table: "Vendors",
                column: "VendorId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Currencies",
                table: "Currencies",
                column: "CurrencyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxRates",
                table: "TaxRates",
                column: "TaxRateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesInvoices",
                table: "SalesInvoices",
                column: "SalesInvoiceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesInvoiceLines",
                table: "SalesInvoiceLines",
                column: "LineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseInvoices",
                table: "PurchaseInvoices",
                column: "PurchaseInvoiceId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseInvoiceLines",
                table: "PurchaseInvoiceLines",
                column: "LineId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods",
                column: "MethodId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountingSettings",
                table: "AccountingSettings",
                column: "SettingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccountingPeriods",
                table: "AccountingPeriods",
                column: "PeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_bills_Vendors_vendor_id",
                table: "bills",
                column: "vendor_id",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_Vendors_vendor_id",
                table: "payments",
                column: "vendor_id",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoiceLines_PurchaseInvoices_PurchaseInvoiceId",
                table: "PurchaseInvoiceLines",
                column: "PurchaseInvoiceId",
                principalTable: "PurchaseInvoices",
                principalColumn: "PurchaseInvoiceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoiceLines_chart_of_accounts_AccountId",
                table: "PurchaseInvoiceLines",
                column: "AccountId",
                principalTable: "chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseInvoices_Vendors_VendorId",
                table: "PurchaseInvoices",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "VendorId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId",
                table: "SalesInvoiceLines",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "SalesInvoiceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoiceLines_chart_of_accounts_AccountId",
                table: "SalesInvoiceLines",
                column: "AccountId",
                principalTable: "chart_of_accounts",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Customers_CustomerId",
                table: "SalesInvoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "CustomerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
