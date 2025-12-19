using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PrimeAppBooks.Data;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Services
{
    public class DatabaseSetup
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IProgress<string> _progress;

        public DatabaseSetup(IServiceProvider serviceProvider, IProgress<string> progress = null)
        {
            _serviceProvider = serviceProvider;
            _progress = progress;
        }

        public async Task<bool> InitializeAccountingDatabaseAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                ReportProgress("Initializing database...");

                // Ensure database is created/migrated (though App.xaml.cs likely does this too)
                // We rely on migrations having run.

                await PopulateReferenceDataAsync(context);
                await CreateAccountingTriggersAsync(context);

                ReportProgress("Database initialization completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database initialization failed: {ex}");
                ReportProgress($"Initialization failed: {ex.Message}");
                return false;
            }
        }

        private void ReportProgress(string message)
        {
            _progress?.Report(message);
            Debug.WriteLine(message);
        }

        private async Task PopulateReferenceDataAsync(AppDbContext context)
        {
            ReportProgress("Populating reference data...");

            await PopulateChartOfAccountsAsync(context);
            await PopulatePaymentMethodsAsync(context);
            await PopulateCurrenciesAsync(context);
            await PopulateTaxRatesAsync(context);
            await PopulateAccountingPeriodsAsync(context);
            await PopulateAccountingSettingsAsync(context);
            
            await context.SaveChangesAsync();
        }

        private async Task PopulateChartOfAccountsAsync(AppDbContext context)
        {
            if (await context.ChartOfAccounts.AnyAsync()) return;

            var accounts = new List<ChartOfAccount>
            {
                // Current Assets
                new() { AccountNumber = "1000", AccountName = "Cash", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1010", AccountName = "Petty Cash", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1020", AccountName = "Bank - Checking", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1030", AccountName = "Bank - Savings", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1100", AccountName = "Accounts Receivable", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1110", AccountName = "Allowance for Doubtful Accounts", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "1200", AccountName = "Inventory", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1300", AccountName = "Prepaid Expenses", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1310", AccountName = "Prepaid Insurance", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1320", AccountName = "Prepaid Rent", AccountType = "ASSET", AccountSubtype = "CURRENT_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },

                // Non-Current Assets
                new() { AccountNumber = "1400", AccountName = "Property, Plant & Equipment", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1410", AccountName = "Land", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1420", AccountName = "Buildings", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1430", AccountName = "Equipment", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1440", AccountName = "Vehicles", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1450", AccountName = "Furniture & Fixtures", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1500", AccountName = "Accumulated Depreciation", AccountType = "ASSET", AccountSubtype = "FIXED_ASSET", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "1600", AccountName = "Intangible Assets", AccountType = "ASSET", AccountSubtype = "INTANGIBLE_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1610", AccountName = "Goodwill", AccountType = "ASSET", AccountSubtype = "INTANGIBLE_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "1620", AccountName = "Patents", AccountType = "ASSET", AccountSubtype = "INTANGIBLE_ASSET", NormalBalance = "DEBIT", IsSystemAccount = true },

                // Current Liabilities
                new() { AccountNumber = "2000", AccountName = "Accounts Payable", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2100", AccountName = "Short-term Loans", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2200", AccountName = "Accrued Expenses", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2210", AccountName = "Accrued Salaries", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2220", AccountName = "Accrued Taxes", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2300", AccountName = "Unearned Revenue", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2400", AccountName = "Current Portion of Long-term Debt", AccountType = "LIABILITY", AccountSubtype = "CURRENT_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },

                // Long-term Liabilities
                new() { AccountNumber = "2500", AccountName = "Long-term Loans", AccountType = "LIABILITY", AccountSubtype = "LONG_TERM_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2510", AccountName = "Mortgage Payable", AccountType = "LIABILITY", AccountSubtype = "LONG_TERM_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2520", AccountName = "Bonds Payable", AccountType = "LIABILITY", AccountSubtype = "LONG_TERM_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "2530", AccountName = "Deferred Tax Liability", AccountType = "LIABILITY", AccountSubtype = "LONG_TERM_LIABILITY", NormalBalance = "CREDIT", IsSystemAccount = true },

                // Equity
                new() { AccountNumber = "3000", AccountName = "Common Stock", AccountType = "EQUITY", AccountSubtype = "CAPITAL", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "3010", AccountName = "Preferred Stock", AccountType = "EQUITY", AccountSubtype = "CAPITAL", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "3020", AccountName = "Additional Paid-in Capital", AccountType = "EQUITY", AccountSubtype = "CAPITAL", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "3100", AccountName = "Retained Earnings", AccountType = "EQUITY", AccountSubtype = "RETAINED_EARNINGS", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "3200", AccountName = "Current Year Earnings", AccountType = "EQUITY", AccountSubtype = "NET_INCOME", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "3300", AccountName = "Dividends", AccountType = "EQUITY", AccountSubtype = "DIVIDENDS", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "3400", AccountName = "Treasury Stock", AccountType = "EQUITY", AccountSubtype = "TREASURY_STOCK", NormalBalance = "DEBIT", IsSystemAccount = true },

                // Revenue
                new() { AccountNumber = "4000", AccountName = "Sales Revenue", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4010", AccountName = "Product Sales", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4100", AccountName = "Service Revenue", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4200", AccountName = "Interest Income", AccountType = "REVENUE", AccountSubtype = "OTHER_INCOME", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4210", AccountName = "Dividend Income", AccountType = "REVENUE", AccountSubtype = "OTHER_INCOME", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4220", AccountName = "Gain on Sale of Assets", AccountType = "REVENUE", AccountSubtype = "OTHER_INCOME", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4300", AccountName = "Sales Returns and Allowances", AccountType = "REVENUE", AccountSubtype = "CONTRA_REVENUE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "4310", AccountName = "Sales Discounts", AccountType = "REVENUE", AccountSubtype = "CONTRA_REVENUE", NormalBalance = "DEBIT", IsSystemAccount = true },

                // Cost of Goods Sold
                new() { AccountNumber = "5000", AccountName = "Cost of Goods Sold", AccountType = "EXPENSE", AccountSubtype = "COGS", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5010", AccountName = "Purchases", AccountType = "EXPENSE", AccountSubtype = "COGS", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5020", AccountName = "Freight-In", AccountType = "EXPENSE", AccountSubtype = "COGS", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5030", AccountName = "Purchase Returns and Allowances", AccountType = "EXPENSE", AccountSubtype = "COGS", NormalBalance = "CREDIT", IsSystemAccount = true },

                // Operating Expenses
                new() { AccountNumber = "5100", AccountName = "Salaries and Wages", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5110", AccountName = "Employee Benefits", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5120", AccountName = "Payroll Taxes", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5200", AccountName = "Rent Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5300", AccountName = "Utilities Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5310", AccountName = "Telephone Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5320", AccountName = "Internet Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5400", AccountName = "Depreciation Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5410", AccountName = "Amortization Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5500", AccountName = "Office Supplies", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5600", AccountName = "Insurance Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5700", AccountName = "Advertising Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5800", AccountName = "Repairs and Maintenance", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5900", AccountName = "Professional Fees", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },

                // Non-Operating Expenses
                new() { AccountNumber = "6000", AccountName = "Interest Expense", AccountType = "EXPENSE", AccountSubtype = "FINANCIAL_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "6100", AccountName = "Loss on Sale of Assets", AccountType = "EXPENSE", AccountSubtype = "OTHER_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "6200", AccountName = "Income Tax Expense", AccountType = "EXPENSE", AccountSubtype = "TAX_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true }
            };

            await context.ChartOfAccounts.AddRangeAsync(accounts);
        }

        private async Task PopulatePaymentMethodsAsync(AppDbContext context)
        {
            if (await context.PaymentMethods.AnyAsync()) return;

            var methods = new List<PaymentMethod>
            {
                new() { MethodName = "Cash", MethodCode = "CASH" },
                new() { MethodName = "Check", MethodCode = "CHECK" },
                new() { MethodName = "Credit Card", MethodCode = "CREDIT_CARD" },
                new() { MethodName = "Bank Transfer", MethodCode = "BANK_TRANSFER" },
                new() { MethodName = "Digital Wallet", MethodCode = "DIGITAL_WALLET" }
            };

            await context.PaymentMethods.AddRangeAsync(methods);
        }

        private async Task PopulateCurrenciesAsync(AppDbContext context)
        {
            if (await context.Currencies.AnyAsync()) return;

            var currencies = new List<Currency>
            {
                new() { CurrencyCode = "USD", CurrencyName = "US Dollar", Symbol = "$", IsBaseCurrency = true },
                new() { CurrencyCode = "EUR", CurrencyName = "Euro", Symbol = "€", IsBaseCurrency = false },
                new() { CurrencyCode = "GBP", CurrencyName = "British Pound", Symbol = "£", IsBaseCurrency = false },
                new() { CurrencyCode = "JPY", CurrencyName = "Japanese Yen", Symbol = "¥", IsBaseCurrency = false },
                new() { CurrencyCode = "CAD", CurrencyName = "Canadian Dollar", Symbol = "C$", IsBaseCurrency = false }
            };

            await context.Currencies.AddRangeAsync(currencies);
        }

        private async Task PopulateTaxRatesAsync(AppDbContext context)
        {
            if (await context.TaxRates.AnyAsync()) return;

            var rates = new List<TaxRate>
            {
                new() { TaxName = "Standard Sales Tax", TaxCode = "SALES_STANDARD", Rate = 8.0000m, TaxType = "SALES", EffectiveFrom = new DateTime(2024, 1, 1).ToUniversalTime() },
                new() { TaxName = "Reduced Sales Tax", TaxCode = "SALES_REDUCED", Rate = 5.0000m, TaxType = "SALES", EffectiveFrom = new DateTime(2024, 1, 1).ToUniversalTime() },
                new() { TaxName = "Zero Sales Tax", TaxCode = "SALES_ZERO", Rate = 0.0000m, TaxType = "SALES", EffectiveFrom = new DateTime(2024, 1, 1).ToUniversalTime() },
                new() { TaxName = "Input VAT", TaxCode = "VAT_INPUT", Rate = 15.0000m, TaxType = "VAT", EffectiveFrom = new DateTime(2024, 1, 1).ToUniversalTime() },
                new() { TaxName = "Output VAT", TaxCode = "VAT_OUTPUT", Rate = 15.0000m, TaxType = "VAT", EffectiveFrom = new DateTime(2024, 1, 1).ToUniversalTime() }
            };

            await context.TaxRates.AddRangeAsync(rates);
        }

        private async Task PopulateAccountingPeriodsAsync(AppDbContext context)
        {
            if (await context.AccountingPeriods.AnyAsync()) return;

            int currentYear = DateTime.Now.Year;
            var periods = new List<AccountingPeriod>();

            string[] monthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

            for (int i = 0; i < 12; i++)
            {
                var startDate = new DateTime(currentYear, i + 1, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                periods.Add(new AccountingPeriod
                {
                    PeriodName = $"{monthNames[i]} {currentYear}",
                    StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
                    FiscalYear = currentYear,
                    PeriodNumber = i + 1
                });
            }

            // Year End Period
            periods.Add(new AccountingPeriod
            {
                PeriodName = $"Year End {currentYear}",
                StartDate = DateTime.SpecifyKind(new DateTime(currentYear, 1, 1), DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(new DateTime(currentYear, 12, 31), DateTimeKind.Utc),
                FiscalYear = currentYear,
                PeriodNumber = 13
            });

            await context.AccountingPeriods.AddRangeAsync(periods);
        }

        private async Task PopulateAccountingSettingsAsync(AppDbContext context)
        {
            if (await context.AccountingSettings.AnyAsync()) return;

            var settings = new List<AccountingSetting>
            {
                new() { SettingKey = "company_name", SettingValue = "Your Company Name", Description = "Legal name of the company" },
                new() { SettingKey = "fiscal_year_start", SettingValue = "1", Description = "First month of fiscal year (1=January)" },
                new() { SettingKey = "base_currency", SettingValue = "USD", Description = "Base currency for accounting" },
                new() { SettingKey = "tax_calculation_method", SettingValue = "EXCLUSIVE", Description = "Tax calculation method (INCLUSIVE/EXCLUSIVE)" },
                new() { SettingKey = "invoice_terms", SettingValue = "Net 30", Description = "Default invoice payment terms" },
                new() { SettingKey = "bill_terms", SettingValue = "Net 30", Description = "Default bill payment terms" },
                new() { SettingKey = "auto_number_journals", SettingValue = "true", Description = "Automatically number journal entries" },
                new() { SettingKey = "require_journal_approval", SettingValue = "false", Description = "Require approval for journal entries" },
                new() { SettingKey = "decimal_places", SettingValue = "2", Description = "Number of decimal places for amounts" },
                new() { SettingKey = "inventory_valuation_method", SettingValue = "FIFO", Description = "Inventory valuation method (FIFO/LIFO/AVERAGE)" },
                new() { SettingKey = "depreciation_method", SettingValue = "STRAIGHT_LINE", Description = "Default depreciation method" },
                new() { SettingKey = "financial_year_end", SettingValue = "12", Description = "Last month of financial year" }
            };

            await context.AccountingSettings.AddRangeAsync(settings);
        }

        private async Task CreateAccountingTriggersAsync(AppDbContext context)
        {
            ReportProgress("Creating accounting triggers...");

            // IMPORTANT: These triggers maintain 'CurrentBalance' on ChartOfAccounts.
            // Using ExecuteSqlRawAsync to ensure valid SQL execution.

            const string triggerFuncSql = @"
                CREATE OR REPLACE FUNCTION update_account_balance()
                RETURNS TRIGGER
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_OP = 'INSERT' THEN
                        UPDATE ""ChartOfAccounts""
                        SET ""CurrentBalance"" = ""CurrentBalance"" + NEW.""DebitAmount"" - NEW.""CreditAmount""
                        WHERE ""AccountId"" = NEW.""AccountId"";
                    ELSIF TG_OP = 'UPDATE' THEN
                        UPDATE ""ChartOfAccounts""
                        SET ""CurrentBalance"" = ""CurrentBalance"" - OLD.""DebitAmount"" + OLD.""CreditAmount""
                        WHERE ""AccountId"" = OLD.""AccountId"";

                        UPDATE ""ChartOfAccounts""
                        SET ""CurrentBalance"" = ""CurrentBalance"" + NEW.""DebitAmount"" - NEW.""CreditAmount""
                        WHERE ""AccountId"" = NEW.""AccountId"";
                    ELSIF TG_OP = 'DELETE' THEN
                        UPDATE ""ChartOfAccounts""
                        SET ""CurrentBalance"" = ""CurrentBalance"" - OLD.""DebitAmount"" + OLD.""CreditAmount""
                        WHERE ""AccountId"" = OLD.""AccountId"";
                    END IF;
                    RETURN NULL;
                END;
                $$;";

            await context.Database.ExecuteSqlRawAsync(triggerFuncSql);

            // Trigger for JournalLines
            // Note: EF Core quotes table names by default, e.g., "JournalLines".
            // We need to ensure the trigger uses the correct table name.
            
            const string triggerSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'update_account_balance_trigger') THEN
                        CREATE TRIGGER update_account_balance_trigger
                        AFTER INSERT OR UPDATE OR DELETE ON ""JournalLines""
                        FOR EACH ROW EXECUTE FUNCTION update_account_balance();
                    END IF;
                END $$;";

            await context.Database.ExecuteSqlRawAsync(triggerSql);
            
            // Note: Removed number generation trigger as standard identity columns or app logic can handle it, 
            // or explicitly rely on user inputs for AccountNumber (which ChartOfAccounts entity requires).
            // Creating triggers for other tables (invoices, etc.) if needed can be added here,
            // but typical EF apps handle logic in services.
        }
    }
}