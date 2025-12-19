using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class CreateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    PeriodId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.PeriodId);
                });

            migrationBuilder.CreateTable(
                name: "AccountingSettings",
                columns: table => new
                {
                    SettingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SettingKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingSettings", x => x.SettingId);
                });

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    account_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    account_subtype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    description = table.Column<string>(type: "text", nullable: true),
                    parent_account_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_system_account = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    normal_balance = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "DEBIT"),
                    opening_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    opening_balance_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    created_by = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chart_of_accounts", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_chart_of_accounts_chart_of_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    CurrencyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CurrencyName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.CurrencyId);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    CustomerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContactPerson = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    BillingAddress = table.Column<string>(type: "text", nullable: false),
                    ShippingAddress = table.Column<string>(type: "text", nullable: false),
                    TaxId = table.Column<string>(type: "text", nullable: false),
                    DefaultRevenueAccountId = table.Column<int>(type: "integer", nullable: true),
                    DefaultPaymentTermsId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "journal_entries",
                columns: table => new
                {
                    journal_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    journal_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: true),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    description = table.Column<string>(type: "text", nullable: false),
                    journal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "GENERAL"),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    posted_by = table.Column<int>(type: "integer", nullable: true),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entries", x => x.journal_id);
                });

            migrationBuilder.CreateTable(
                name: "journal_templates",
                columns: table => new
                {
                    template_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    template_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    journal_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    template_data = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_templates", x => x.template_id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    MethodId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MethodName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MethodCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.MethodId);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    TaxRateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaxName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.TaxRateId);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    VendorId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VendorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VendorCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContactPerson = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TaxId = table.Column<string>(type: "text", nullable: false),
                    DefaultExpenseAccountId = table.Column<int>(type: "integer", nullable: true),
                    DefaultPaymentTermsId = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.VendorId);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoices",
                columns: table => new
                {
                    SalesInvoiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountReceived = table.Column<decimal>(type: "numeric", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Terms = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoices", x => x.SalesInvoiceId);
                    table.ForeignKey(
                        name: "FK_SalesInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journal_lines",
                columns: table => new
                {
                    line_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_id = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    line_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: true),
                    debit_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    credit_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    cost_center_id = table.Column<int>(type: "integer", nullable: true),
                    project_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_lines", x => x.line_id);
                    table.ForeignKey(
                        name: "FK_journal_lines_chart_of_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_lines_journal_entries_journal_id",
                        column: x => x.journal_id,
                        principalTable: "journal_entries",
                        principalColumn: "journal_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bills",
                columns: table => new
                {
                    bill_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bill_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vendor_id = table.Column<int>(type: "integer", nullable: false),
                    bill_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    terms = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bills", x => x.bill_id);
                    table.ForeignKey(
                        name: "FK_bills_Vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoices",
                columns: table => new
                {
                    PurchaseInvoiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VendorId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Terms = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoices", x => x.PurchaseInvoiceId);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoices_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesInvoiceLines",
                columns: table => new
                {
                    LineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SalesInvoiceId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesInvoiceLines", x => x.LineId);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId",
                        column: x => x.SalesInvoiceId,
                        principalTable: "SalesInvoices",
                        principalColumn: "SalesInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesInvoiceLines_chart_of_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    vendor_id = table.Column<int>(type: "integer", nullable: false),
                    bill_id = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    memo = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDING"),
                    bank_account_id = table.Column<int>(type: "integer", nullable: true),
                    processed_by = table.Column<int>(type: "integer", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK_payments_Vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "Vendors",
                        principalColumn: "VendorId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payments_bills_bill_id",
                        column: x => x.bill_id,
                        principalTable: "bills",
                        principalColumn: "bill_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseInvoiceLines",
                columns: table => new
                {
                    LineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseInvoiceId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseInvoiceLines", x => x.LineId);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_PurchaseInvoices_PurchaseInvoiceId",
                        column: x => x.PurchaseInvoiceId,
                        principalTable: "PurchaseInvoices",
                        principalColumn: "PurchaseInvoiceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseInvoiceLines_chart_of_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bills_vendor_id",
                table: "bills",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "idx_chart_of_accounts_number",
                table: "chart_of_accounts",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_chart_of_accounts_parent",
                table: "chart_of_accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "idx_chart_of_accounts_type",
                table: "chart_of_accounts",
                columns: new[] { "account_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_date",
                table: "journal_entries",
                column: "journal_date");

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_number",
                table: "journal_entries",
                column: "journal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_period",
                table: "journal_entries",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_status",
                table: "journal_entries",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_journal_lines_account_date",
                table: "journal_lines",
                columns: new[] { "account_id", "line_date" });

            migrationBuilder.CreateIndex(
                name: "idx_journal_lines_journal",
                table: "journal_lines",
                column: "journal_id");

            migrationBuilder.CreateIndex(
                name: "idx_journal_lines_period",
                table: "journal_lines",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "idx_journal_templates_active",
                table: "journal_templates",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_journal_templates_type",
                table: "journal_templates",
                column: "journal_type");

            migrationBuilder.CreateIndex(
                name: "IX_payments_bill_id",
                table: "payments",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_vendor_id",
                table: "payments",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_AccountId",
                table: "PurchaseInvoiceLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoiceLines_PurchaseInvoiceId",
                table: "PurchaseInvoiceLines",
                column: "PurchaseInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseInvoices_VendorId",
                table: "PurchaseInvoices",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_AccountId",
                table: "SalesInvoiceLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoiceLines_SalesInvoiceId",
                table: "SalesInvoiceLines",
                column: "SalesInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_CustomerId",
                table: "SalesInvoices",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "AccountingSettings");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "journal_lines");

            migrationBuilder.DropTable(
                name: "journal_templates");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "PurchaseInvoiceLines");

            migrationBuilder.DropTable(
                name: "SalesInvoiceLines");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropTable(
                name: "journal_entries");

            migrationBuilder.DropTable(
                name: "bills");

            migrationBuilder.DropTable(
                name: "PurchaseInvoices");

            migrationBuilder.DropTable(
                name: "SalesInvoices");

            migrationBuilder.DropTable(
                name: "chart_of_accounts");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
