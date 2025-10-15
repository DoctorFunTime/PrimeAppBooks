using Npgsql;
using PrimeAppBooks.Configurations;
using System.Diagnostics;

namespace PrimeAppBooks.Services
{
    public class DatabaseSetup
    {
        private readonly string _connectionString;
        private readonly BoxServices _messageBoxService = new();
        private readonly IProgress<string> _progress;

        public DatabaseSetup(IProgress<string> progress = null)
        {
            _connectionString = AppConfig.ConnectionString;
            _progress = progress;
        }

        public async Task<bool> InitializeAccountingDatabaseAsync()
        {
            await using var conn = new NpgsqlConnection(_connectionString);

            try
            {
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();

                ReportProgress("Starting database initialization...");

                try
                {
                    // Phase 1: Drop existing functions (safely)
                    await DropAccountingFunctionsAsync();

                    // Phase 2: Create core accounting tables
                    await CreateCoreTablesAsync();

                    // Phase 3: Create transaction tables
                    await CreateTransactionTablesAsync();

                    // Phase 4: Schema adjustments
                    await AdjustAccountingSchemaAsync();

                    // Phase 5: Populate reference data
                    await PopulateAccountingReferenceDataAsync();

                    // Phase 6: Remove triggers from audit tables first
                    await RemoveAccountingAuditTriggersAsync();

                    // Phase 7: Create triggers and procedures
                    await CreateAccountingTriggersAsync();
                    await AddAccountingTriggersToTablesAsync();

                    await tx.CommitAsync();
                    ReportProgress("Database initialization completed successfully!");
                    return true;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    Debug.WriteLine($"Database initialization failed: {ex}");
                    ReportProgress($"Initialization failed: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection failed: {ex}");
                ReportProgress($"Connection failed: {ex.Message}");
                return false;
            }
        }

        private void ReportProgress(string message)
        {
            _progress?.Report(message);
            Debug.WriteLine(message);
        }

        private async Task CreateCoreTablesAsync()
        {
            ReportProgress("Creating core accounting tables...");

            // Phase 1: Base tables with no dependencies
            await Task.WhenAll(
                CreateChartOfAccountsTableAsync(),
                CreateAccountingPeriodsTableAsync(),
                CreateVendorsTableAsync(),
                CreateCustomersTableAsync(),
                CreateBankAccountsTableAsync(),
                CreateTaxRatesTableAsync(),
                CreateCostCentersTableAsync(),
                CreatePaymentMethodsTableAsync(),
                CreateCurrenciesTableAsync(),
                CreateAccountingSettingsTableAsync()
            );
            ReportProgress("Created base tables (Phase 1/5)");

            // Phase 2: Tables depending on Phase 1
            await Task.WhenAll(
                CreateJournalEntriesTableAsync(),
                CreateAccountsPayableTableAsync(),
                CreateAccountsReceivableTableAsync(),
                CreateInvoicesTableAsync(),
                CreateBillsTableAsync(),
                CreateBankReconciliationsTableAsync(),
                CreateTaxTransactionsTableAsync(),
                CreateFixedAssetsTableAsync(),
                CreateBudgetTableAsync(),
                CreateProjectsTableAsync(),
                CreateExpenseCategoriesTableAsync(),
                CreateExchangeRatesTableAsync()
            );
            ReportProgress("Created dependent tables (Phase 2/5)");

            // Phase 3: Tables depending on Phase 2 (especially journal_entries)
            await Task.WhenAll(
                CreateGeneralLedgerTableAsync(),
                CreatePaymentsTableAsync(),
                CreateReceiptsTableAsync(),
                CreateDepreciationTableAsync(),
                CreateBudgetLinesTableAsync(),
                CreateFinancialReportsTableAsync()
            );
            ReportProgress("Created ledger and related tables (Phase 3/5)");

            // Phase 4: Report tables
            await Task.WhenAll(
                CreateTrialBalanceTableAsync(),
                CreateBalanceSheetTableAsync(),
                CreateIncomeStatementTableAsync(),
                CreateCashFlowTableAsync()
            );
            ReportProgress("Created report tables (Phase 4/5)");

            ReportProgress("Core tables creation completed (Phase 5/5)");
        }

        private async Task CreateTransactionTablesAsync()
        {
            ReportProgress("Creating transaction tables...");

            // Phase 1: Base transaction tables (no dependencies within this phase)
            await Task.WhenAll(
                CreatePurchaseOrdersTableAsync(),
                CreateSalesOrdersTableAsync(),
                CreateInventoryAccountingTableAsync(),
                CreateLoanAccountingTableAsync(),
                CreateInvestmentAccountingTableAsync(),
                CreateAuditLogTableAsync()
            );
            ReportProgress("Created base transaction tables (Phase 1/3)");

            // Phase 2: Line items and tables depending on journal_entries
            await Task.WhenAll(
                CreatePurchaseOrderLinesTableAsync(),
                CreateSalesOrderLinesTableAsync(),
                CreateInvoiceLinesTableAsync(),
                CreateBillLinesTableAsync(),
                CreatePayrollAccountingTableAsync(),
                CreateClosingEntriesTableAsync(),
                CreateFinancialRatiosTableAsync()
            );
            ReportProgress("Created dependent transaction tables (Phase 2/3)");

            // Phase 3: Tables depending on financial_reports
            await CreateNotesToFinancialsTableAsync();
            ReportProgress("Created notes to financials table (Phase 3/3)");
        }

        private async Task DropAccountingFunctionsAsync()
        {
            const string dropFunctionsSql = @"
                DROP FUNCTION IF EXISTS public.update_account_balance CASCADE;
                DROP FUNCTION IF EXISTS public.generate_account_number CASCADE;
                DROP FUNCTION IF EXISTS public.calculate_depreciation CASCADE;
                DROP FUNCTION IF EXISTS public.post_journal_entry CASCADE;
                DROP FUNCTION IF EXISTS public.accounting_audit_trigger CASCADE;
                DROP FUNCTION IF EXISTS public.notify_trigger_v2 CASCADE;
                DROP FUNCTION IF EXISTS public.log_user_activity_v2 CASCADE;";

            await ExecuteNonQuerySafeAsync(dropFunctionsSql, "Dropping existing functions");
        }

        // CORE ACCOUNTING TABLES
        private async Task CreateChartOfAccountsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS chart_of_accounts (
                    account_id SERIAL PRIMARY KEY,
                    account_number VARCHAR(20) UNIQUE NOT NULL,
                    account_name VARCHAR(255) NOT NULL,
                    account_type VARCHAR(50) NOT NULL CHECK (account_type IN
                        ('ASSET', 'LIABILITY', 'EQUITY', 'REVENUE', 'EXPENSE')),
                    account_subtype VARCHAR(50),
                    description TEXT,
                    parent_account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    is_active BOOLEAN DEFAULT TRUE,
                    is_system_account BOOLEAN DEFAULT FALSE,
                    normal_balance VARCHAR(10) CHECK (normal_balance IN ('DEBIT', 'CREDIT')),
                    opening_balance DECIMAL(18,2) DEFAULT 0,
                    opening_balance_date DATE,
                    current_balance DECIMAL(18,2) DEFAULT 0,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating chart_of_accounts table");
        }

        private async Task CreateAccountingPeriodsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS accounting_periods (
                    period_id SERIAL PRIMARY KEY,
                    period_name VARCHAR(100) NOT NULL,
                    start_date DATE NOT NULL,
                    end_date DATE NOT NULL,
                    fiscal_year INTEGER NOT NULL,
                    period_number INTEGER NOT NULL CHECK (period_number BETWEEN 1 AND 13),
                    is_closed BOOLEAN DEFAULT FALSE,
                    closed_by INTEGER,
                    closed_at TIMESTAMP,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(fiscal_year, period_number)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating accounting_periods table");
        }

        private async Task CreateJournalEntriesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS journal_entries (
                    journal_id SERIAL PRIMARY KEY,
                    journal_number VARCHAR(50) UNIQUE NOT NULL,
                    journal_date DATE NOT NULL,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    reference VARCHAR(100),
                    description TEXT NOT NULL,
                    journal_type VARCHAR(50) NOT NULL CHECK (journal_type IN
                        ('GENERAL', 'SALES', 'PURCHASE', 'CASH_RECEIPT', 'CASH_DISBURSEMENT', 'ADJUSTING', 'CLOSING')),
                    amount DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'POSTED', 'VOID')),
                    posted_by INTEGER,
                    posted_at TIMESTAMP,
                    created_by INTEGER NOT NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating journal_entries table");
        }

        private async Task CreateGeneralLedgerTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS general_ledger (
                    ledger_id SERIAL PRIMARY KEY,
                    journal_id INTEGER REFERENCES journal_entries(journal_id),
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    ledger_date DATE NOT NULL,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    debit_amount DECIMAL(18,2) DEFAULT 0,
                    credit_amount DECIMAL(18,2) DEFAULT 0,
                    description TEXT,
                    reference VARCHAR(100),
                    cost_center_id INTEGER,
                    project_id INTEGER,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating general_ledger table");
        }

        private async Task CreateAccountsPayableTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS accounts_payable (
                    payable_id SERIAL PRIMARY KEY,
                    vendor_id INTEGER NOT NULL,
                    bill_id INTEGER,
                    due_date DATE NOT NULL,
                    amount_due DECIMAL(18,2) NOT NULL,
                    amount_paid DECIMAL(18,2) DEFAULT 0,
                    balance DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'OPEN' CHECK (status IN ('OPEN', 'PARTIAL', 'PAID', 'OVERDUE', 'VOID')),
                    days_overdue INTEGER DEFAULT 0,
                    last_payment_date DATE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating accounts_payable table");
        }

        private async Task CreateAccountsReceivableTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS accounts_receivable (
                    receivable_id SERIAL PRIMARY KEY,
                    customer_id INTEGER NOT NULL,
                    invoice_id INTEGER,
                    due_date DATE NOT NULL,
                    amount_due DECIMAL(18,2) NOT NULL,
                    amount_paid DECIMAL(18,2) DEFAULT 0,
                    balance DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'OPEN' CHECK (status IN ('OPEN', 'PARTIAL', 'PAID', 'OVERDUE', 'BAD_DEBT')),
                    days_overdue INTEGER DEFAULT 0,
                    last_payment_date DATE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating accounts_receivable table");
        }

        private async Task CreateVendorsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS vendors (
                    vendor_id SERIAL PRIMARY KEY,
                    vendor_code VARCHAR(50) UNIQUE NOT NULL,
                    vendor_name VARCHAR(255) NOT NULL,
                    contact_person VARCHAR(255),
                    email VARCHAR(255),
                    phone VARCHAR(50),
                    address TEXT,
                    tax_id VARCHAR(100),
                    payment_terms VARCHAR(100),
                    credit_limit DECIMAL(18,2),
                    current_balance DECIMAL(18,2) DEFAULT 0,
                    account_number VARCHAR(50),
                    bank_name VARCHAR(255),
                    is_active BOOLEAN DEFAULT TRUE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating vendors table");
        }

        private async Task CreateCustomersTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS customers (
                    customer_id SERIAL PRIMARY KEY,
                    customer_code VARCHAR(50) UNIQUE NOT NULL,
                    customer_name VARCHAR(255) NOT NULL,
                    contact_person VARCHAR(255),
                    email VARCHAR(255),
                    phone VARCHAR(50),
                    address TEXT,
                    tax_id VARCHAR(100),
                    payment_terms VARCHAR(100),
                    credit_limit DECIMAL(18,2),
                    current_balance DECIMAL(18,2) DEFAULT 0,
                    account_number VARCHAR(50),
                    bank_name VARCHAR(255),
                    customer_type VARCHAR(50) DEFAULT 'REGULAR',
                    is_active BOOLEAN DEFAULT TRUE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating customers table");
        }

        private async Task CreateInvoicesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS invoices (
                    invoice_id SERIAL PRIMARY KEY,
                    invoice_number VARCHAR(100) UNIQUE NOT NULL,
                    customer_id INTEGER REFERENCES customers(customer_id),
                    invoice_date DATE NOT NULL,
                    due_date DATE NOT NULL,
                    total_amount DECIMAL(18,2) NOT NULL,
                    tax_amount DECIMAL(18,2) DEFAULT 0,
                    discount_amount DECIMAL(18,2) DEFAULT 0,
                    net_amount DECIMAL(18,2) NOT NULL,
                    amount_paid DECIMAL(18,2) DEFAULT 0,
                    balance DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'SENT', 'PARTIAL', 'PAID', 'OVERDUE', 'VOID')),
                    terms TEXT,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating invoices table");
        }

        private async Task CreateBillsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS bills (
                    bill_id SERIAL PRIMARY KEY,
                    bill_number VARCHAR(100) UNIQUE NOT NULL,
                    vendor_id INTEGER REFERENCES vendors(vendor_id),
                    bill_date DATE NOT NULL,
                    due_date DATE NOT NULL,
                    total_amount DECIMAL(18,2) NOT NULL,
                    tax_amount DECIMAL(18,2) DEFAULT 0,
                    discount_amount DECIMAL(18,2) DEFAULT 0,
                    net_amount DECIMAL(18,2) NOT NULL,
                    amount_paid DECIMAL(18,2) DEFAULT 0,
                    balance DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'RECEIVED', 'PARTIAL', 'PAID', 'OVERDUE', 'VOID')),
                    terms TEXT,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating bills table");
        }

        private async Task CreatePaymentsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS payments (
                    payment_id SERIAL PRIMARY KEY,
                    payment_number VARCHAR(100) UNIQUE NOT NULL,
                    payment_date DATE NOT NULL,
                    vendor_id INTEGER REFERENCES vendors(vendor_id),
                    bill_id INTEGER REFERENCES bills(bill_id),
                    payment_method VARCHAR(50) NOT NULL,
                    amount DECIMAL(18,2) NOT NULL,
                    reference_number VARCHAR(100),
                    memo TEXT,
                    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'PROCESSED', 'VOID')),
                    bank_account_id INTEGER,
                    processed_by INTEGER,
                    processed_at TIMESTAMP,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating payments table");
        }

        private async Task CreateReceiptsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS receipts (
                    receipt_id SERIAL PRIMARY KEY,
                    receipt_number VARCHAR(100) UNIQUE NOT NULL,
                    receipt_date DATE NOT NULL,
                    customer_id INTEGER REFERENCES customers(customer_id),
                    invoice_id INTEGER REFERENCES invoices(invoice_id),
                    payment_method VARCHAR(50) NOT NULL,
                    amount DECIMAL(18,2) NOT NULL,
                    reference_number VARCHAR(100),
                    memo TEXT,
                    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'PROCESSED', 'VOID')),
                    bank_account_id INTEGER,
                    processed_by INTEGER,
                    processed_at TIMESTAMP,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating receipts table");
        }

        private async Task CreateBankAccountsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS bank_accounts (
                    bank_account_id SERIAL PRIMARY KEY,
                    account_name VARCHAR(255) NOT NULL,
                    account_number VARCHAR(100) NOT NULL,
                    bank_name VARCHAR(255) NOT NULL,
                    branch_name VARCHAR(255),
                    account_type VARCHAR(50) CHECK (account_type IN ('CHECKING', 'SAVINGS', 'MONEY_MARKET')),
                    routing_number VARCHAR(50),
                    swift_code VARCHAR(50),
                    iban VARCHAR(50),
                    currency_code VARCHAR(3) DEFAULT 'USD',
                    current_balance DECIMAL(18,2) DEFAULT 0,
                    ledger_balance DECIMAL(18,2) DEFAULT 0,
                    is_active BOOLEAN DEFAULT TRUE,
                    opened_date DATE,
                    closed_date DATE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating bank_accounts table");
        }

        private async Task CreateBankReconciliationsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS bank_reconciliations (
                    reconciliation_id SERIAL PRIMARY KEY,
                    bank_account_id INTEGER REFERENCES bank_accounts(bank_account_id),
                    statement_date DATE NOT NULL,
                    statement_balance DECIMAL(18,2) NOT NULL,
                    ledger_balance DECIMAL(18,2) NOT NULL,
                    reconciled_balance DECIMAL(18,2) NOT NULL,
                    outstanding_deposits DECIMAL(18,2) DEFAULT 0,
                    outstanding_checks DECIMAL(18,2) DEFAULT 0,
                    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'IN_PROGRESS', 'COMPLETED')),
                    reconciled_by INTEGER,
                    reconciled_at TIMESTAMP,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating bank_reconciliations table");
        }

        private async Task CreateTaxRatesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS tax_rates (
                    tax_rate_id SERIAL PRIMARY KEY,
                    tax_name VARCHAR(255) NOT NULL,
                    tax_code VARCHAR(50) UNIQUE NOT NULL,
                    tax_rate DECIMAL(8,4) NOT NULL,
                    tax_type VARCHAR(50) CHECK (tax_type IN ('SALES', 'PURCHASE', 'VAT', 'GST', 'INCOME')),
                    is_compound BOOLEAN DEFAULT FALSE,
                    is_active BOOLEAN DEFAULT TRUE,
                    effective_from DATE NOT NULL,
                    effective_to DATE,
                    description TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating tax_rates table");
        }

        private async Task CreateTaxTransactionsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS tax_transactions (
                    tax_transaction_id SERIAL PRIMARY KEY,
                    transaction_date DATE NOT NULL,
                    transaction_type VARCHAR(50) NOT NULL,
                    transaction_id INTEGER NOT NULL,
                    tax_rate_id INTEGER REFERENCES tax_rates(tax_rate_id),
                    taxable_amount DECIMAL(18,2) NOT NULL,
                    tax_amount DECIMAL(18,2) NOT NULL,
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'PAID', 'REFUNDED')),
                    due_date DATE,
                    paid_date DATE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating tax_transactions table");
        }

        private async Task CreateFixedAssetsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS fixed_assets (
                    asset_id SERIAL PRIMARY KEY,
                    asset_code VARCHAR(100) UNIQUE NOT NULL,
                    asset_name VARCHAR(255) NOT NULL,
                    asset_category VARCHAR(100) NOT NULL,
                    purchase_date DATE NOT NULL,
                    purchase_cost DECIMAL(18,2) NOT NULL,
                    salvage_value DECIMAL(18,2) DEFAULT 0,
                    useful_life_years INTEGER NOT NULL,
                    depreciation_method VARCHAR(50) DEFAULT 'STRAIGHT_LINE' CHECK (depreciation_method IN
                        ('STRAIGHT_LINE', 'DECLINING_BALANCE', 'SUM_OF_YEARS')),
                    current_value DECIMAL(18,2) NOT NULL,
                    accumulated_depreciation DECIMAL(18,2) DEFAULT 0,
                    location VARCHAR(255),
                    serial_number VARCHAR(100),
                    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE', 'SOLD', 'RETIRED', 'DISPOSED')),
                    disposed_date DATE,
                    disposed_amount DECIMAL(18,2),
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating fixed_assets table");
        }

        private async Task CreateDepreciationTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS depreciation (
                    depreciation_id SERIAL PRIMARY KEY,
                    asset_id INTEGER REFERENCES fixed_assets(asset_id),
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    depreciation_date DATE NOT NULL,
                    depreciation_amount DECIMAL(18,2) NOT NULL,
                    accumulated_depreciation DECIMAL(18,2) NOT NULL,
                    remaining_value DECIMAL(18,2) NOT NULL,
                    journal_id INTEGER REFERENCES journal_entries(journal_id),
                    is_posted BOOLEAN DEFAULT FALSE,
                    posted_at TIMESTAMP,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating depreciation table");
        }

        private async Task CreateBudgetTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS budget (
                    budget_id SERIAL PRIMARY KEY,
                    budget_name VARCHAR(255) NOT NULL,
                    budget_type VARCHAR(50) CHECK (budget_type IN ('OPERATING', 'CAPITAL', 'CASH_FLOW')),
                    fiscal_year INTEGER NOT NULL,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'APPROVED', 'ACTIVE', 'CLOSED')),
                    total_amount DECIMAL(18,2) NOT NULL,
                    actual_amount DECIMAL(18,2) DEFAULT 0,
                    variance_amount DECIMAL(18,2) DEFAULT 0,
                    approved_by INTEGER,
                    approved_at TIMESTAMP,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating budget table");
        }

        private async Task CreateBudgetLinesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS budget_lines (
                    budget_line_id SERIAL PRIMARY KEY,
                    budget_id INTEGER REFERENCES budget(budget_id),
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    budget_amount DECIMAL(18,2) NOT NULL,
                    actual_amount DECIMAL(18,2) DEFAULT 0,
                    variance_amount DECIMAL(18,2) DEFAULT 0,
                    variance_percentage DECIMAL(8,2) DEFAULT 0,
                    cost_center_id INTEGER,
                    project_id INTEGER,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating budget_lines table");
        }

        // FINANCIAL REPORTING TABLES
        private async Task CreateFinancialReportsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS financial_reports (
                    report_id SERIAL PRIMARY KEY,
                    report_name VARCHAR(255) NOT NULL,
                    report_type VARCHAR(50) CHECK (report_type IN
                        ('BALANCE_SHEET', 'INCOME_STATEMENT', 'CASH_FLOW', 'TRIAL_BALANCE', 'BUDGET_VARIANCE',
                         'RETAINED_EARNINGS', 'CHANGES_IN_EQUITY', 'NOTES_TO_FINANCIAL_STATEMENTS')),
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    generated_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    report_data JSONB,
                    generated_by INTEGER,
                    is_final BOOLEAN DEFAULT FALSE,
                    is_audited BOOLEAN DEFAULT FALSE,
                    auditor_notes TEXT,
                    approval_status VARCHAR(20) DEFAULT 'DRAFT' CHECK (approval_status IN ('DRAFT', 'REVIEWED', 'APPROVED')),
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating financial_reports table");
        }

        private async Task CreateTrialBalanceTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS trial_balance (
                    trial_balance_id SERIAL PRIMARY KEY,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    account_number VARCHAR(20) NOT NULL,
                    account_name VARCHAR(255) NOT NULL,
                    debit_balance DECIMAL(18,2) DEFAULT 0,
                    credit_balance DECIMAL(18,2) DEFAULT 0,
                    beginning_debit DECIMAL(18,2) DEFAULT 0,
                    beginning_credit DECIMAL(18,2) DEFAULT 0,
                    period_debit DECIMAL(18,2) DEFAULT 0,
                    period_credit DECIMAL(18,2) DEFAULT 0,
                    ending_debit DECIMAL(18,2) DEFAULT 0,
                    ending_credit DECIMAL(18,2) DEFAULT 0,
                    generated_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    is_adjusted BOOLEAN DEFAULT FALSE,
                    is_post_closing BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating trial_balance table");
        }

        private async Task CreateBalanceSheetTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS balance_sheet (
                    balance_sheet_id SERIAL PRIMARY KEY,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    report_date DATE NOT NULL,
                    -- Assets
                    current_assets DECIMAL(18,2) NOT NULL DEFAULT 0,
                    cash_and_equivalents DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accounts_receivable DECIMAL(18,2) NOT NULL DEFAULT 0,
                    inventory DECIMAL(18,2) NOT NULL DEFAULT 0,
                    prepaid_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
                    non_current_assets DECIMAL(18,2) NOT NULL DEFAULT 0,
                    fixed_assets DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accumulated_depreciation DECIMAL(18,2) NOT NULL DEFAULT 0,
                    intangible_assets DECIMAL(18,2) NOT NULL DEFAULT 0,
                    total_assets DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Liabilities
                    current_liabilities DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accounts_payable DECIMAL(18,2) NOT NULL DEFAULT 0,
                    short_term_debt DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accrued_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
                    non_current_liabilities DECIMAL(18,2) NOT NULL DEFAULT 0,
                    long_term_debt DECIMAL(18,2) NOT NULL DEFAULT 0,
                    deferred_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,
                    total_liabilities DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Equity
                    contributed_capital DECIMAL(18,2) NOT NULL DEFAULT 0,
                    retained_earnings DECIMAL(18,2) NOT NULL DEFAULT 0,
                    treasury_stock DECIMAL(18,2) NOT NULL DEFAULT 0,
                    current_earnings DECIMAL(18,2) NOT NULL DEFAULT 0,
                    total_equity DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Verification
                    assets_total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    liabilities_equity_total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    is_balanced BOOLEAN DEFAULT FALSE,

                    generated_by INTEGER,
                    generated_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    is_final BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating balance_sheet table");
        }

        private async Task CreateIncomeStatementTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS income_statement (
                    income_statement_id SERIAL PRIMARY KEY,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    report_date DATE NOT NULL,

                    -- Revenue Section
                    revenue_total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    gross_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
                    sales_returns DECIMAL(18,2) NOT NULL DEFAULT 0,
                    sales_discounts DECIMAL(18,2) NOT NULL DEFAULT 0,
                    net_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Cost of Goods Sold
                    cogs_total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    beginning_inventory DECIMAL(18,2) NOT NULL DEFAULT 0,
                    purchases DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ending_inventory DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Gross Profit
                    gross_profit DECIMAL(18,2) NOT NULL DEFAULT 0,
                    gross_margin_percentage DECIMAL(8,2) NOT NULL DEFAULT 0,

                    -- Operating Expenses
                    operating_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
                    salaries_wages DECIMAL(18,2) NOT NULL DEFAULT 0,
                    rent_expense DECIMAL(18,2) NOT NULL DEFAULT 0,
                    utilities_expense DECIMAL(18,2) NOT NULL DEFAULT 0,
                    depreciation_amortization DECIMAL(18,2) NOT NULL DEFAULT 0,
                    marketing_expense DECIMAL(18,2) NOT NULL DEFAULT 0,
                    professional_fees DECIMAL(18,2) NOT NULL DEFAULT 0,
                    other_operating_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Operating Income
                    operating_income DECIMAL(18,2) NOT NULL DEFAULT 0,
                    operating_margin_percentage DECIMAL(8,2) NOT NULL DEFAULT 0,

                    -- Non-Operating Items
                    other_income DECIMAL(18,2) NOT NULL DEFAULT 0,
                    interest_income DECIMAL(18,2) NOT NULL DEFAULT 0,
                    other_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
                    interest_expense DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Final Calculations
                    net_income_before_tax DECIMAL(18,2) NOT NULL DEFAULT 0,
                    tax_expense DECIMAL(18,2) NOT NULL DEFAULT 0,
                    net_income DECIMAL(18,2) NOT NULL DEFAULT 0,
                    net_margin_percentage DECIMAL(8,2) NOT NULL DEFAULT 0,

                    -- EBITDA Calculation
                    ebitda DECIMAL(18,2) NOT NULL DEFAULT 0,

                    generated_by INTEGER,
                    generated_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    is_final BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating income_statement table");
        }

        private async Task CreateCashFlowTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS cash_flow_statement (
                    cash_flow_id SERIAL PRIMARY KEY,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    report_date DATE NOT NULL,

                    -- Operating Activities
                    operating_activities DECIMAL(18,2) NOT NULL DEFAULT 0,
                    net_income DECIMAL(18,2) NOT NULL DEFAULT 0,
                    depreciation_amortization DECIMAL(18,2) NOT NULL DEFAULT 0,
                    changes_in_working_capital DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accounts_receivable_change DECIMAL(18,2) NOT NULL DEFAULT 0,
                    inventory_change DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accounts_payable_change DECIMAL(18,2) NOT NULL DEFAULT 0,
                    accrued_expenses_change DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Investing Activities
                    investing_activities DECIMAL(18,2) NOT NULL DEFAULT 0,
                    capital_expenditures DECIMAL(18,2) NOT NULL DEFAULT 0,
                    asset_sales DECIMAL(18,2) NOT NULL DEFAULT 0,
                    investment_purchases DECIMAL(18,2) NOT NULL DEFAULT 0,
                    investment_sales DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Financing Activities
                    financing_activities DECIMAL(18,2) NOT NULL DEFAULT 0,
                    debt_issued DECIMAL(18,2) NOT NULL DEFAULT 0,
                    debt_repayments DECIMAL(18,2) NOT NULL DEFAULT 0,
                    equity_issued DECIMAL(18,2) NOT NULL DEFAULT 0,
                    dividends_paid DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Net Change
                    net_cash_flow DECIMAL(18,2) NOT NULL DEFAULT 0,
                    beginning_cash DECIMAL(18,2) NOT NULL DEFAULT 0,
                    ending_cash DECIMAL(18,2) NOT NULL DEFAULT 0,

                    -- Free Cash Flow
                    free_cash_flow DECIMAL(18,2) NOT NULL DEFAULT 0,

                    generated_by INTEGER,
                    generated_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    is_final BOOLEAN DEFAULT FALSE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating cash_flow_statement table");
        }

        private async Task CreateCostCentersTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS cost_centers (
                    cost_center_id SERIAL PRIMARY KEY,
                    cost_center_code VARCHAR(50) UNIQUE NOT NULL,
                    cost_center_name VARCHAR(255) NOT NULL,
                    description TEXT,
                    parent_center_id INTEGER REFERENCES cost_centers(cost_center_id),
                    is_active BOOLEAN DEFAULT TRUE,
                    manager_id INTEGER,
                    budget DECIMAL(18,2) DEFAULT 0,
                    actual_spending DECIMAL(18,2) DEFAULT 0,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating cost_centers table");
        }

        private async Task CreateProjectsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS projects (
                    project_id SERIAL PRIMARY KEY,
                    project_code VARCHAR(50) UNIQUE NOT NULL,
                    project_name VARCHAR(255) NOT NULL,
                    description TEXT,
                    start_date DATE,
                    end_date DATE,
                    budget DECIMAL(18,2) DEFAULT 0,
                    actual_cost DECIMAL(18,2) DEFAULT 0,
                    status VARCHAR(20) DEFAULT 'PLANNING' CHECK (status IN
                        ('PLANNING', 'ACTIVE', 'ON_HOLD', 'COMPLETED', 'CANCELLED')),
                    project_manager INTEGER,
                    customer_id INTEGER REFERENCES customers(customer_id),
                    is_billable BOOLEAN DEFAULT FALSE,
                    billing_rate DECIMAL(18,2) DEFAULT 0,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating projects table");
        }

        private async Task CreateExpenseCategoriesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS expense_categories (
                    category_id SERIAL PRIMARY KEY,
                    category_name VARCHAR(255) NOT NULL,
                    description TEXT,
                    parent_category_id INTEGER REFERENCES expense_categories(category_id),
                    is_active BOOLEAN DEFAULT TRUE,
                    default_account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating expense_categories table");
        }

        private async Task CreatePaymentMethodsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS payment_methods (
                    method_id SERIAL PRIMARY KEY,
                    method_name VARCHAR(100) NOT NULL,
                    method_code VARCHAR(50) UNIQUE NOT NULL,
                    description TEXT,
                    is_active BOOLEAN DEFAULT TRUE,
                    requires_bank_account BOOLEAN DEFAULT FALSE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating payment_methods table");
        }

        private async Task CreateCurrenciesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS currencies (
                    currency_id SERIAL PRIMARY KEY,
                    currency_code VARCHAR(3) UNIQUE NOT NULL,
                    currency_name VARCHAR(100) NOT NULL,
                    symbol VARCHAR(10),
                    decimal_places INTEGER DEFAULT 2,
                    is_active BOOLEAN DEFAULT TRUE,
                    is_base_currency BOOLEAN DEFAULT FALSE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating currencies table");
        }

        private async Task CreateExchangeRatesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS exchange_rates (
                    exchange_rate_id SERIAL PRIMARY KEY,
                    from_currency VARCHAR(3) NOT NULL,
                    to_currency VARCHAR(3) NOT NULL,
                    exchange_rate DECIMAL(18,6) NOT NULL,
                    rate_date DATE NOT NULL,
                    is_active BOOLEAN DEFAULT TRUE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(from_currency, to_currency, rate_date)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating exchange_rates table");
        }

        private async Task CreateAccountingSettingsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS accounting_settings (
                    setting_id SERIAL PRIMARY KEY,
                    setting_key VARCHAR(255) UNIQUE NOT NULL,
                    setting_value TEXT,
                    setting_type VARCHAR(50) DEFAULT 'STRING',
                    description TEXT,
                    is_active BOOLEAN DEFAULT TRUE,
                    updated_by INTEGER,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating accounting_settings table");
        }

        // TRANSACTION TABLES
        private async Task CreatePurchaseOrdersTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS purchase_orders (
                    po_id SERIAL PRIMARY KEY,
                    po_number VARCHAR(100) UNIQUE NOT NULL,
                    vendor_id INTEGER REFERENCES vendors(vendor_id),
                    po_date DATE NOT NULL,
                    expected_delivery_date DATE,
                    total_amount DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN
                        ('DRAFT', 'SENT', 'CONFIRMED', 'RECEIVED', 'CANCELLED')),
                    terms TEXT,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating purchase_orders table");
        }

        private async Task CreatePurchaseOrderLinesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS purchase_order_lines (
                    po_line_id SERIAL PRIMARY KEY,
                    po_id INTEGER REFERENCES purchase_orders(po_id) ON DELETE CASCADE,
                    line_number INTEGER NOT NULL,
                    item_description VARCHAR(500) NOT NULL,
                    item_code VARCHAR(100),
                    quantity DECIMAL(18,2) NOT NULL,
                    unit_price DECIMAL(18,2) NOT NULL,
                    discount_percentage DECIMAL(8,2) DEFAULT 0,
                    discount_amount DECIMAL(18,2) DEFAULT 0,
                    tax_amount DECIMAL(18,2) DEFAULT 0,
                    line_total DECIMAL(18,2) NOT NULL,
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    tax_rate_id INTEGER REFERENCES tax_rates(tax_rate_id),
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(po_id, line_number)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating purchase_order_lines table");
        }

        private async Task CreateSalesOrdersTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS sales_orders (
                    so_id SERIAL PRIMARY KEY,
                    so_number VARCHAR(100) UNIQUE NOT NULL,
                    customer_id INTEGER REFERENCES customers(customer_id),
                    so_date DATE NOT NULL,
                    delivery_date DATE,
                    total_amount DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN
                        ('DRAFT', 'CONFIRMED', 'SHIPPED', 'DELIVERED', 'INVOICED', 'CANCELLED')),
                    terms TEXT,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating sales_orders table");
        }

        private async Task CreateSalesOrderLinesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS sales_order_lines (
                    so_line_id SERIAL PRIMARY KEY,
                    so_id INTEGER REFERENCES sales_orders(so_id) ON DELETE CASCADE,
                    line_number INTEGER NOT NULL,
                    item_description VARCHAR(500) NOT NULL,
                    item_code VARCHAR(100),
                    quantity DECIMAL(18,2) NOT NULL,
                    unit_price DECIMAL(18,2) NOT NULL,
                    discount_percentage DECIMAL(8,2) DEFAULT 0,
                    discount_amount DECIMAL(18,2) DEFAULT 0,
                    tax_amount DECIMAL(18,2) DEFAULT 0,
                    line_total DECIMAL(18,2) NOT NULL,
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    tax_rate_id INTEGER REFERENCES tax_rates(tax_rate_id),
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(so_id, line_number)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating sales_order_lines table");
        }

        private async Task CreateInvoiceLinesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS invoice_lines (
                    invoice_line_id SERIAL PRIMARY KEY,
                    invoice_id INTEGER REFERENCES invoices(invoice_id) ON DELETE CASCADE,
                    line_number INTEGER NOT NULL,
                    item_description VARCHAR(500) NOT NULL,
                    item_code VARCHAR(100),
                    quantity DECIMAL(18,2) NOT NULL,
                    unit_price DECIMAL(18,2) NOT NULL,
                    discount_percentage DECIMAL(8,2) DEFAULT 0,
                    discount_amount DECIMAL(18,2) DEFAULT 0,
                    tax_amount DECIMAL(18,2) DEFAULT 0,
                    line_total DECIMAL(18,2) NOT NULL,
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    tax_rate_id INTEGER REFERENCES tax_rates(tax_rate_id),
                    cost_center_id INTEGER,
                    project_id INTEGER,
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(invoice_id, line_number)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating invoice_lines table");
        }

        private async Task CreateBillLinesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS bill_lines (
                    bill_line_id SERIAL PRIMARY KEY,
                    bill_id INTEGER REFERENCES bills(bill_id) ON DELETE CASCADE,
                    line_number INTEGER NOT NULL,
                    item_description VARCHAR(500) NOT NULL,
                    item_code VARCHAR(100),
                    quantity DECIMAL(18,2) NOT NULL,
                    unit_price DECIMAL(18,2) NOT NULL,
                    discount_percentage DECIMAL(8,2) DEFAULT 0,
                    discount_amount DECIMAL(18,2) DEFAULT 0,
                    tax_amount DECIMAL(18,2) DEFAULT 0,
                    line_total DECIMAL(18,2) NOT NULL,
                    account_id INTEGER REFERENCES chart_of_accounts(account_id),
                    tax_rate_id INTEGER REFERENCES tax_rates(tax_rate_id),
                    cost_center_id INTEGER,
                    project_id INTEGER,
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(bill_id, line_number)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating bill_lines table");
        }

        private async Task CreateInventoryAccountingTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS inventory_accounting (
                    inventory_id SERIAL PRIMARY KEY,
                    item_code VARCHAR(100) NOT NULL,
                    item_name VARCHAR(255) NOT NULL,
                    category VARCHAR(100),
                    unit_cost DECIMAL(18,2) NOT NULL,
                    quantity_on_hand INTEGER DEFAULT 0,
                    quantity_committed INTEGER DEFAULT 0,
                    quantity_available INTEGER DEFAULT 0,
                    total_value DECIMAL(18,2) DEFAULT 0,
                    reorder_point INTEGER DEFAULT 0,
                    preferred_vendor_id INTEGER REFERENCES vendors(vendor_id),
                    is_active BOOLEAN DEFAULT TRUE,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating inventory_accounting table");
        }

        private async Task CreatePayrollAccountingTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS payroll_accounting (
                    payroll_id SERIAL PRIMARY KEY,
                    payroll_period VARCHAR(100) NOT NULL,
                    period_start DATE NOT NULL,
                    period_end DATE NOT NULL,
                    employee_id INTEGER NOT NULL,
                    gross_pay DECIMAL(18,2) NOT NULL,
                    deductions DECIMAL(18,2) DEFAULT 0,
                    net_pay DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'DRAFT' CHECK (status IN ('DRAFT', 'PROCESSED', 'PAID')),
                    payment_date DATE,
                    journal_id INTEGER REFERENCES journal_entries(journal_id),
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating payroll_accounting table");
        }

        private async Task CreateLoanAccountingTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS loan_accounting (
                    loan_id SERIAL PRIMARY KEY,
                    loan_number VARCHAR(100) UNIQUE NOT NULL,
                    loan_type VARCHAR(50) CHECK (loan_type IN ('PAYABLE', 'RECEIVABLE')),
                    principal_amount DECIMAL(18,2) NOT NULL,
                    interest_rate DECIMAL(8,4) NOT NULL,
                    term_months INTEGER NOT NULL,
                    start_date DATE NOT NULL,
                    end_date DATE NOT NULL,
                    monthly_payment DECIMAL(18,2) NOT NULL,
                    remaining_balance DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE', 'PAID_OFF', 'DEFAULTED')),
                    customer_id INTEGER REFERENCES customers(customer_id),
                    vendor_id INTEGER REFERENCES vendors(vendor_id),
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating loan_accounting table");
        }

        private async Task CreateInvestmentAccountingTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS investment_accounting (
                    investment_id SERIAL PRIMARY KEY,
                    investment_number VARCHAR(100) UNIQUE NOT NULL,
                    investment_type VARCHAR(50) NOT NULL,
                    principal_amount DECIMAL(18,2) NOT NULL,
                    interest_rate DECIMAL(8,4) NOT NULL,
                    start_date DATE NOT NULL,
                    maturity_date DATE NOT NULL,
                    current_value DECIMAL(18,2) NOT NULL,
                    status VARCHAR(20) DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE', 'MATURED', 'SOLD')),
                    financial_institution VARCHAR(255),
                    account_number VARCHAR(100),
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating investment_accounting table");
        }

        private async Task CreateAuditLogTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS audit_log (
                    audit_id BIGSERIAL PRIMARY KEY,
                    table_name VARCHAR(100) NOT NULL,
                    record_id INTEGER NOT NULL,
                    operation VARCHAR(20) NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
                    old_values JSONB,
                    new_values JSONB,
                    changed_by INTEGER,
                    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    ip_address VARCHAR(50),
                    user_agent TEXT,
                    session_id VARCHAR(255)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating audit_log table");
        }

        private async Task CreateClosingEntriesTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS closing_entries (
                    closing_id SERIAL PRIMARY KEY,
                    fiscal_year INTEGER NOT NULL,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    closing_date DATE NOT NULL,
                    closing_type VARCHAR(50) CHECK (closing_type IN
                        ('TEMPORARY_ACCOUNTS', 'INCOME_SUMMARY', 'RETAINED_EARNINGS')),
                    journal_id INTEGER REFERENCES journal_entries(journal_id),
                    net_income DECIMAL(18,2),
                    retained_earnings_before DECIMAL(18,2),
                    retained_earnings_after DECIMAL(18,2),
                    status VARCHAR(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'POSTED', 'REVERSED')),
                    closed_by INTEGER,
                    closed_at TIMESTAMP,
                    reversed_by INTEGER,
                    reversed_at TIMESTAMP,
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE(fiscal_year, closing_type)
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating closing_entries table");
        }

        // CRUCIAL FINANCIAL STATEMENT SUPPORT TABLES
        private async Task CreateFinancialRatiosTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS financial_ratios (
                    ratio_id SERIAL PRIMARY KEY,
                    period_id INTEGER REFERENCES accounting_periods(period_id),
                    report_date DATE NOT NULL,

                    -- Liquidity Ratios
                    current_ratio DECIMAL(8,2),
                    quick_ratio DECIMAL(8,2),
                    cash_ratio DECIMAL(8,2),

                    -- Profitability Ratios
                    gross_margin DECIMAL(8,2),
                    operating_margin DECIMAL(8,2),
                    net_margin DECIMAL(8,2),
                    return_on_assets DECIMAL(8,2),
                    return_on_equity DECIMAL(8,2),

                    -- Efficiency Ratios
                    asset_turnover DECIMAL(8,2),
                    inventory_turnover DECIMAL(8,2),
                    receivables_turnover DECIMAL(8,2),
                    days_sales_outstanding DECIMAL(8,2),

                    -- Leverage Ratios
                    debt_to_equity DECIMAL(8,2),
                    debt_to_assets DECIMAL(8,2),
                    interest_coverage DECIMAL(8,2),

                    -- Market Ratios (if applicable)
                    earnings_per_share DECIMAL(8,2),
                    price_to_earnings DECIMAL(8,2),

                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating financial_ratios table");
        }

        private async Task CreateNotesToFinancialsTableAsync()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS notes_to_financials (
                    note_id SERIAL PRIMARY KEY,
                    report_id INTEGER REFERENCES financial_reports(report_id),
                    note_number VARCHAR(10) NOT NULL,
                    note_title VARCHAR(255) NOT NULL,
                    note_content TEXT NOT NULL,
                    note_category VARCHAR(50) CHECK (note_category IN (
                        'SIGNIFICANT_ACCOUNTING_POLICIES',
                        'REVENUE_RECOGNITION',
                        'INVENTORY',
                        'PROPERTY_PLANT_EQUIPMENT',
                        'INTANGIBLE_ASSETS',
                        'LEASES',
                        'DEBT',
                        'TAXES',
                        'CONTINGENCIES',
                        'RELATED_PARTY_TRANSACTIONS',
                        'SUBSEQUENT_EVENTS'
                    )),
                    display_order INTEGER NOT NULL,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await ExecuteNonQuerySafeAsync(sql, "Creating notes_to_financials table");
        }

        private async Task AdjustAccountingSchemaAsync()
        {
            ReportProgress("Adjusting accounting schema...");

            // Add any schema adjustments needed
            await AddAccountingConstraintsAsync();
            await AddIndexesForPerformanceAsync();
        }

        private async Task AddAccountingConstraintsAsync()
        {
            ReportProgress("Adding accounting constraints...");

            // Use conditional constraint creation to avoid errors
            var constraints = new[]
            {
                @"DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_journal_amount_positive') THEN
                        ALTER TABLE journal_entries ADD CONSTRAINT chk_journal_amount_positive CHECK (amount >= 0);
                    END IF;
                END $$;",

                @"DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_debit_credit_not_both_zero') THEN
                        ALTER TABLE general_ledger ADD CONSTRAINT chk_debit_credit_not_both_zero
                        CHECK (debit_amount != 0 OR credit_amount != 0);
                    END IF;
                END $$;",

                @"DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_invoice_amounts') THEN
                        ALTER TABLE invoices ADD CONSTRAINT chk_invoice_amounts
                        CHECK (total_amount >= 0 AND net_amount >= 0 AND balance >= 0);
                    END IF;
                END $$;",

                @"DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'chk_bill_amounts') THEN
                        ALTER TABLE bills ADD CONSTRAINT chk_bill_amounts
                        CHECK (total_amount >= 0 AND net_amount >= 0 AND balance >= 0);
                    END IF;
                END $$;"
            };

            foreach (var constraint in constraints)
            {
                await ExecuteNonQuerySafeAsync(constraint, "Adding constraints");
            }
        }

        private async Task AddIndexesForPerformanceAsync()
        {
            ReportProgress("Creating performance indexes...");

            const string sql = @"
                -- Create indexes for better performance (only if they don't exist)
                CREATE INDEX IF NOT EXISTS idx_general_ledger_account_date
                ON general_ledger(account_id, ledger_date);

                CREATE INDEX IF NOT EXISTS idx_general_ledger_period
                ON general_ledger(period_id);

                CREATE INDEX IF NOT EXISTS idx_journal_entries_date
                ON journal_entries(journal_date);

                CREATE INDEX IF NOT EXISTS idx_journal_entries_period
                ON journal_entries(period_id);

                CREATE INDEX IF NOT EXISTS idx_invoices_customer_status
                ON invoices(customer_id, status);

                CREATE INDEX IF NOT EXISTS idx_invoice_lines_invoice
                ON invoice_lines(invoice_id);

                CREATE INDEX IF NOT EXISTS idx_bills_vendor_status
                ON bills(vendor_id, status);

                CREATE INDEX IF NOT EXISTS idx_bill_lines_bill
                ON bill_lines(bill_id);

                CREATE INDEX IF NOT EXISTS idx_accounts_receivable_customer
                ON accounts_receivable(customer_id, status);

                CREATE INDEX IF NOT EXISTS idx_accounts_payable_vendor
                ON accounts_payable(vendor_id, status);

                CREATE INDEX IF NOT EXISTS idx_chart_of_accounts_type
                ON chart_of_accounts(account_type, is_active);

                CREATE INDEX IF NOT EXISTS idx_chart_of_accounts_parent
                ON chart_of_accounts(parent_account_id);

                CREATE INDEX IF NOT EXISTS idx_audit_log_table_record
                ON audit_log(table_name, record_id);

                CREATE INDEX IF NOT EXISTS idx_audit_log_changed_at
                ON audit_log(changed_at);

                CREATE INDEX IF NOT EXISTS idx_trial_balance_period
                ON trial_balance(period_id);

                CREATE INDEX IF NOT EXISTS idx_balance_sheet_period
                ON balance_sheet(period_id);

                CREATE INDEX IF NOT EXISTS idx_income_statement_period
                ON income_statement(period_id);

                CREATE INDEX IF NOT EXISTS idx_cash_flow_period
                ON cash_flow_statement(period_id);

                CREATE INDEX IF NOT EXISTS idx_financial_ratios_period
                ON financial_ratios(period_id);

                CREATE INDEX IF NOT EXISTS idx_notes_to_financials_report
                ON notes_to_financials(report_id);

                CREATE INDEX IF NOT EXISTS idx_purchase_orders_vendor
                ON purchase_orders(vendor_id);

                CREATE INDEX IF NOT EXISTS idx_sales_orders_customer
                ON sales_orders(customer_id);

                CREATE INDEX IF NOT EXISTS idx_payments_vendor
                ON payments(vendor_id);

                CREATE INDEX IF NOT EXISTS idx_receipts_customer
                ON receipts(customer_id);";

            await ExecuteNonQuerySafeAsync(sql, "Creating performance indexes");
        }

        private async Task PopulateAccountingReferenceDataAsync()
        {
            ReportProgress("Populating reference data...");

            var tasks = new[]
            {
                PopulateChartOfAccountsAsync(),
                PopulatePaymentMethodsAsync(),
                PopulateCurrenciesAsync(),
                PopulateTaxRatesAsync(),
                PopulateAccountingPeriodsAsync(),
                PopulateAccountingSettingsAsync()
            };

            await Task.WhenAll(tasks);
        }

        private async Task PopulateChartOfAccountsAsync()
        {
            const string sql = @"
                INSERT INTO chart_of_accounts (account_number, account_name, account_type, account_subtype, normal_balance, is_system_account) VALUES
                -- Current Assets
                ('1000', 'Cash', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1010', 'Petty Cash', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1020', 'Bank - Checking', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1030', 'Bank - Savings', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1100', 'Accounts Receivable', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1110', 'Allowance for Doubtful Accounts', 'ASSET', 'CURRENT_ASSET', 'CREDIT', TRUE),
                ('1200', 'Inventory', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1300', 'Prepaid Expenses', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1310', 'Prepaid Insurance', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),
                ('1320', 'Prepaid Rent', 'ASSET', 'CURRENT_ASSET', 'DEBIT', TRUE),

                -- Non-Current Assets
                ('1400', 'Property, Plant & Equipment', 'ASSET', 'FIXED_ASSET', 'DEBIT', TRUE),
                ('1410', 'Land', 'ASSET', 'FIXED_ASSET', 'DEBIT', TRUE),
                ('1420', 'Buildings', 'ASSET', 'FIXED_ASSET', 'DEBIT', TRUE),
                ('1430', 'Equipment', 'ASSET', 'FIXED_ASSET', 'DEBIT', TRUE),
                ('1440', 'Vehicles', 'ASSET', 'FIXED_ASSET', 'DEBIT', TRUE),
                ('1450', 'Furniture & Fixtures', 'ASSET', 'FIXED_ASSET', 'DEBIT', TRUE),
                ('1500', 'Accumulated Depreciation', 'ASSET', 'FIXED_ASSET', 'CREDIT', TRUE),
                ('1600', 'Intangible Assets', 'ASSET', 'INTANGIBLE_ASSET', 'DEBIT', TRUE),
                ('1610', 'Goodwill', 'ASSET', 'INTANGIBLE_ASSET', 'DEBIT', TRUE),
                ('1620', 'Patents', 'ASSET', 'INTANGIBLE_ASSET', 'DEBIT', TRUE),

                -- Current Liabilities
                ('2000', 'Accounts Payable', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),
                ('2100', 'Short-term Loans', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),
                ('2200', 'Accrued Expenses', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),
                ('2210', 'Accrued Salaries', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),
                ('2220', 'Accrued Taxes', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),
                ('2300', 'Unearned Revenue', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),
                ('2400', 'Current Portion of Long-term Debt', 'LIABILITY', 'CURRENT_LIABILITY', 'CREDIT', TRUE),

                -- Long-term Liabilities
                ('2500', 'Long-term Loans', 'LIABILITY', 'LONG_TERM_LIABILITY', 'CREDIT', TRUE),
                ('2510', 'Mortgage Payable', 'LIABILITY', 'LONG_TERM_LIABILITY', 'CREDIT', TRUE),
                ('2520', 'Bonds Payable', 'LIABILITY', 'LONG_TERM_LIABILITY', 'CREDIT', TRUE),
                ('2530', 'Deferred Tax Liability', 'LIABILITY', 'LONG_TERM_LIABILITY', 'CREDIT', TRUE),

                -- Equity
                ('3000', 'Common Stock', 'EQUITY', 'CAPITAL', 'CREDIT', TRUE),
                ('3010', 'Preferred Stock', 'EQUITY', 'CAPITAL', 'CREDIT', TRUE),
                ('3020', 'Additional Paid-in Capital', 'EQUITY', 'CAPITAL', 'CREDIT', TRUE),
                ('3100', 'Retained Earnings', 'EQUITY', 'RETAINED_EARNINGS', 'CREDIT', TRUE),
                ('3200', 'Current Year Earnings', 'EQUITY', 'NET_INCOME', 'CREDIT', TRUE),
                ('3300', 'Dividends', 'EQUITY', 'DIVIDENDS', 'DEBIT', TRUE),
                ('3400', 'Treasury Stock', 'EQUITY', 'TREASURY_STOCK', 'DEBIT', TRUE),

                -- Revenue
                ('4000', 'Sales Revenue', 'REVENUE', 'OPERATING_REVENUE', 'CREDIT', TRUE),
                ('4010', 'Product Sales', 'REVENUE', 'OPERATING_REVENUE', 'CREDIT', TRUE),
                ('4100', 'Service Revenue', 'REVENUE', 'OPERATING_REVENUE', 'CREDIT', TRUE),
                ('4200', 'Interest Income', 'REVENUE', 'OTHER_INCOME', 'CREDIT', TRUE),
                ('4210', 'Dividend Income', 'REVENUE', 'OTHER_INCOME', 'CREDIT', TRUE),
                ('4220', 'Gain on Sale of Assets', 'REVENUE', 'OTHER_INCOME', 'CREDIT', TRUE),
                ('4300', 'Sales Returns and Allowances', 'REVENUE', 'CONTRA_REVENUE', 'DEBIT', TRUE),
                ('4310', 'Sales Discounts', 'REVENUE', 'CONTRA_REVENUE', 'DEBIT', TRUE),

                -- Cost of Goods Sold
                ('5000', 'Cost of Goods Sold', 'EXPENSE', 'COGS', 'DEBIT', TRUE),
                ('5010', 'Purchases', 'EXPENSE', 'COGS', 'DEBIT', TRUE),
                ('5020', 'Freight-In', 'EXPENSE', 'COGS', 'DEBIT', TRUE),
                ('5030', 'Purchase Returns and Allowances', 'EXPENSE', 'COGS', 'CREDIT', TRUE),

                -- Operating Expenses
                ('5100', 'Salaries and Wages', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5110', 'Employee Benefits', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5120', 'Payroll Taxes', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5200', 'Rent Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5300', 'Utilities Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5310', 'Telephone Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5320', 'Internet Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5400', 'Depreciation Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5410', 'Amortization Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5500', 'Office Supplies', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5600', 'Insurance Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5700', 'Advertising Expense', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5800', 'Repairs and Maintenance', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),
                ('5900', 'Professional Fees', 'EXPENSE', 'OPERATING_EXPENSE', 'DEBIT', TRUE),

                -- Non-Operating Expenses
                ('6000', 'Interest Expense', 'EXPENSE', 'FINANCIAL_EXPENSE', 'DEBIT', TRUE),
                ('6100', 'Loss on Sale of Assets', 'EXPENSE', 'OTHER_EXPENSE', 'DEBIT', TRUE),
                ('6200', 'Income Tax Expense', 'EXPENSE', 'TAX_EXPENSE', 'DEBIT', TRUE)
                ON CONFLICT (account_number) DO NOTHING";

            await ExecuteNonQuerySafeAsync(sql, "Populating chart of accounts");
        }

        private async Task PopulatePaymentMethodsAsync()
        {
            const string sql = @"
                INSERT INTO payment_methods (method_name, method_code) VALUES
                ('Cash', 'CASH'),
                ('Check', 'CHECK'),
                ('Credit Card', 'CREDIT_CARD'),
                ('Bank Transfer', 'BANK_TRANSFER'),
                ('Digital Wallet', 'DIGITAL_WALLET')
                ON CONFLICT (method_code) DO NOTHING";

            await ExecuteNonQuerySafeAsync(sql, "Populating payment methods");
        }

        private async Task PopulateCurrenciesAsync()
        {
            const string sql = @"
                INSERT INTO currencies (currency_code, currency_name, symbol, is_base_currency) VALUES
                ('USD', 'US Dollar', '$', TRUE),
                ('EUR', 'Euro', '€', FALSE),
                ('GBP', 'British Pound', '£', FALSE),
                ('JPY', 'Japanese Yen', '¥', FALSE),
                ('CAD', 'Canadian Dollar', 'C$', FALSE)
                ON CONFLICT (currency_code) DO NOTHING";

            await ExecuteNonQuerySafeAsync(sql, "Populating currencies");
        }

        private async Task PopulateTaxRatesAsync()
        {
            const string sql = @"
                INSERT INTO tax_rates (tax_name, tax_code, tax_rate, tax_type, effective_from) VALUES
                ('Standard Sales Tax', 'SALES_STANDARD', 8.0000, 'SALES', '2024-01-01'),
                ('Reduced Sales Tax', 'SALES_REDUCED', 5.0000, 'SALES', '2024-01-01'),
                ('Zero Sales Tax', 'SALES_ZERO', 0.0000, 'SALES', '2024-01-01'),
                ('Input VAT', 'VAT_INPUT', 15.0000, 'VAT', '2024-01-01'),
                ('Output VAT', 'VAT_OUTPUT', 15.0000, 'VAT', '2024-01-01')
                ON CONFLICT (tax_code) DO NOTHING";

            await ExecuteNonQuerySafeAsync(sql, "Populating tax rates");
        }

        private async Task PopulateAccountingPeriodsAsync()
        {
            int currentYear = DateTime.Now.Year;

            string sql = $@"
                INSERT INTO accounting_periods (period_name, start_date, end_date, fiscal_year, period_number) VALUES
                ('January {currentYear}', '{currentYear}-01-01', '{currentYear}-01-31', {currentYear}, 1),
                ('February {currentYear}', '{currentYear}-02-01', '{currentYear}-02-28', {currentYear}, 2),
                ('March {currentYear}', '{currentYear}-03-01', '{currentYear}-03-31', {currentYear}, 3),
                ('April {currentYear}', '{currentYear}-04-01', '{currentYear}-04-30', {currentYear}, 4),
                ('May {currentYear}', '{currentYear}-05-01', '{currentYear}-05-31', {currentYear}, 5),
                ('June {currentYear}', '{currentYear}-06-01', '{currentYear}-06-30', {currentYear}, 6),
                ('July {currentYear}', '{currentYear}-07-01', '{currentYear}-07-31', {currentYear}, 7),
                ('August {currentYear}', '{currentYear}-08-01', '{currentYear}-08-31', {currentYear}, 8),
                ('September {currentYear}', '{currentYear}-09-01', '{currentYear}-09-30', {currentYear}, 9),
                ('October {currentYear}', '{currentYear}-10-01', '{currentYear}-10-31', {currentYear}, 10),
                ('November {currentYear}', '{currentYear}-11-01', '{currentYear}-11-30', {currentYear}, 11),
                ('December {currentYear}', '{currentYear}-12-01', '{currentYear}-12-31', {currentYear}, 12),
                ('Year End {currentYear}', '{currentYear}-01-01', '{currentYear}-12-31', {currentYear}, 13)
                ON CONFLICT (fiscal_year, period_number) DO NOTHING";

            await ExecuteNonQuerySafeAsync(sql, "Populating accounting periods");
        }

        private async Task PopulateAccountingSettingsAsync()
        {
            const string sql = @"
                INSERT INTO accounting_settings (setting_key, setting_value, description) VALUES
                ('company_name', 'Your Company Name', 'Legal name of the company'),
                ('fiscal_year_start', '1', 'First month of fiscal year (1=January)'),
                ('base_currency', 'USD', 'Base currency for accounting'),
                ('tax_calculation_method', 'EXCLUSIVE', 'Tax calculation method (INCLUSIVE/EXCLUSIVE)'),
                ('invoice_terms', 'Net 30', 'Default invoice payment terms'),
                ('bill_terms', 'Net 30', 'Default bill payment terms'),
                ('auto_number_journals', 'true', 'Automatically number journal entries'),
                ('require_journal_approval', 'false', 'Require approval for journal entries'),
                ('decimal_places', '2', 'Number of decimal places for amounts'),
                ('inventory_valuation_method', 'FIFO', 'Inventory valuation method (FIFO/LIFO/AVERAGE)'),
                ('depreciation_method', 'STRAIGHT_LINE', 'Default depreciation method'),
                ('financial_year_end', '12', 'Last month of financial year')
                ON CONFLICT (setting_key) DO NOTHING";

            await ExecuteNonQuerySafeAsync(sql, "Populating accounting settings");
        }

        private async Task RemoveAccountingAuditTriggersAsync()
        {
            const string sql = @"
                -- Drop triggers from audit tables if they exist
                DROP TRIGGER IF EXISTS accounting_audit_journal_entries ON journal_entries;
                DROP TRIGGER IF EXISTS accounting_audit_general_ledger ON general_ledger;
                DROP TRIGGER IF EXISTS accounting_audit_invoices ON invoices;
                DROP TRIGGER IF EXISTS accounting_audit_bills ON bills;";

            await ExecuteNonQuerySafeAsync(sql, "Removing audit triggers");
        }

        private async Task CreateAccountingTriggersAsync()
        {
            ReportProgress("Creating accounting triggers...");

            const string sql = @"
                -- Function to update account balances
                CREATE OR REPLACE FUNCTION update_account_balance()
                RETURNS TRIGGER
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        UPDATE chart_of_accounts
                        SET current_balance = current_balance + NEW.debit_amount - NEW.credit_amount
                        WHERE account_id = NEW.account_id;
                    ELSIF TG_OP = 'UPDATE' THEN
                        UPDATE chart_of_accounts
                        SET current_balance = current_balance - OLD.debit_amount + OLD.credit_amount
                        WHERE account_id = OLD.account_id;

                        UPDATE chart_of_accounts
                        SET current_balance = current_balance + NEW.debit_amount - NEW.credit_amount
                        WHERE account_id = NEW.account_id;
                    ELSIF TG_OP = 'DELETE' THEN
                        UPDATE chart_of_accounts
                        SET current_balance = current_balance - OLD.debit_amount + OLD.credit_amount
                        WHERE account_id = OLD.account_id;
                    END IF;
                    RETURN NULL;
                END;
                $$;

                -- Function to generate account numbers
                CREATE OR REPLACE FUNCTION generate_account_number()
                RETURNS TRIGGER
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    next_number INTEGER;
                BEGIN
                    IF NEW.account_number IS NULL THEN
                        SELECT COALESCE(MAX(CAST(SUBSTRING(account_number FROM '^[0-9]+') AS INTEGER)), 0) + 1
                        INTO next_number
                        FROM chart_of_accounts
                        WHERE account_number ~ '^[0-9]+$';

                        NEW.account_number := LPAD(next_number::TEXT, 4, '0');
                    END IF;
                    RETURN NEW;
                END;
                $$;";

            await ExecuteNonQuerySafeAsync(sql, "Creating accounting triggers");
        }

        private async Task AddAccountingTriggersToTablesAsync()
        {
            ReportProgress("Adding triggers to tables...");

            string[] tables = {
                "chart_of_accounts", "accounting_periods", "journal_entries", "general_ledger",
                "accounts_payable", "accounts_receivable", "vendors", "customers",
                "invoices", "invoice_lines", "bills", "bill_lines", "payments", "receipts",
                "bank_accounts", "bank_reconciliations", "tax_rates", "tax_transactions",
                "fixed_assets", "depreciation", "budget", "budget_lines", "financial_reports",
                "trial_balance", "balance_sheet", "income_statement", "cash_flow_statement",
                "cost_centers", "projects", "expense_categories", "payment_methods",
                "currencies", "exchange_rates", "accounting_settings", "purchase_orders",
                "purchase_order_lines", "sales_orders", "sales_order_lines", "inventory_accounting",
                "payroll_accounting", "loan_accounting", "investment_accounting", "closing_entries",
                "financial_ratios", "notes_to_financials"
            };

            // Process triggers in batches
            for (int i = 0; i < tables.Length; i += 10)
            {
                var batch = tables.Skip(i).Take(10);
                foreach (var table in batch)
                {
                    string sql = $@"
                    DO $$
                    BEGIN
                        -- Create balance update trigger for general_ledger
                        IF '{table}' = 'general_ledger' AND NOT EXISTS (
                            SELECT 1 FROM pg_trigger WHERE tgname = 'update_account_balance_trigger'
                        ) THEN
                            CREATE TRIGGER update_account_balance_trigger
                            AFTER INSERT OR UPDATE OR DELETE ON {table}
                            FOR EACH ROW EXECUTE FUNCTION update_account_balance();
                        END IF;

                        -- Create account number generator trigger
                        IF '{table}' = 'chart_of_accounts' AND NOT EXISTS (
                            SELECT 1 FROM pg_trigger WHERE tgname = 'generate_account_number_trigger'
                        ) THEN
                            CREATE TRIGGER generate_account_number_trigger
                            BEFORE INSERT ON {table}
                            FOR EACH ROW EXECUTE FUNCTION generate_account_number();
                        END IF;
                    END $$;";

                    await ExecuteNonQuerySafeAsync(sql, $"Adding triggers to {table}");
                }
                ReportProgress($"Added triggers to batch {i / 10 + 1}/{(tables.Length + 9) / 10}");
            }
        }

        private async Task ExecuteNonQuerySafeAsync(string sql, string operationName = "")
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            try
            {
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);

                // Set a reasonable timeout to prevent hanging
                cmd.CommandTimeout = 30;

                await cmd.ExecuteNonQueryAsync();

                if (!string.IsNullOrEmpty(operationName))
                {
                    Debug.WriteLine($"Successfully executed: {operationName}");
                }
            }
            catch (PostgresException ex) when (ex.SqlState == "42P07" || ex.SqlState == "42710" || ex.SqlState == "23505")
            {
                // Handle already exists errors gracefully
                Debug.WriteLine($"Object already exists for: {operationName} - {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in {operationName}: {ex.Message}");
                Debug.WriteLine($"SQL: {sql}");
                throw; // Re-throw only critical errors
            }
        }
    }
}