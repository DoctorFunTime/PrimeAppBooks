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
            await context.SaveChangesAsync();

            await EnsureEssentialExpenseAccountsExist(context);
            await context.SaveChangesAsync();

            await PopulatePaymentMethodsAsync(context);
            await context.SaveChangesAsync();

            await PopulateCurrenciesAsync(context);
            await context.SaveChangesAsync();

            await PopulateTaxRatesAsync(context);
            await context.SaveChangesAsync();

            await PopulateAccountingPeriodsAsync(context);
            await context.SaveChangesAsync();

            await PopulateAccountingSettingsAsync(context);
            await context.SaveChangesAsync();

            await PopulateStudentGradesAsync(context);
            await context.SaveChangesAsync();

            await PopulateAssetCategoriesAsync(context);
            await context.SaveChangesAsync();

            await EnsureDefaultAdminUserAsync(context);
            await context.SaveChangesAsync();

            await CreateAccountingTriggersAsync(context);
        }

        private async Task EnsureDefaultAdminUserAsync(AppDbContext context)
        {
            if (!await context.Users.AnyAsync())
            {
                ReportProgress("Seeding default admin user account...");
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    AccountName = "System",
                    AccountSurname = "Administrator",
                    AccountTitle = "System Administrator",
                    AccountType = "Admin",
                    AccountDepartment = "Executive",
                    AccountTasks = true
                };
                context.Users.Add(adminUser);
            }
        }

        private async Task PopulateChartOfAccountsAsync(AppDbContext context)
        {
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
                new() { AccountNumber = "3110", AccountName = "Drawings", AccountType = "EQUITY", AccountSubtype = "Owner's Equity", NormalBalance = "DEBIT", Description = "Withdrawals by the CEO of the school", IsSystemAccount = false },
                new() { AccountNumber = "3200", AccountName = "Current Year Earnings", AccountType = "EQUITY", AccountSubtype = "NET_INCOME", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "3300", AccountName = "Dividends", AccountType = "EQUITY", AccountSubtype = "DIVIDENDS", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "3400", AccountName = "Treasury Stock", AccountType = "EQUITY", AccountSubtype = "TREASURY_STOCK", NormalBalance = "DEBIT", IsSystemAccount = true },

                // Revenue
                new() { AccountNumber = "4000", AccountName = "Sales Revenue", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4010", AccountName = "Product Sales", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4100", AccountName = "Service Revenue", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = true },
                new() { AccountNumber = "4110", AccountName = "Registration Fees Income", AccountType = "REVENUE", AccountSubtype = "OPERATING_REVENUE", NormalBalance = "CREDIT", IsSystemAccount = false, Description = "Student registration fees recorded separately from the students fees account" },
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
                new() { AccountNumber = "5150", AccountName = "Bad Debts Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5200", AccountName = "Rent Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5300", AccountName = "Utilities Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5310", AccountName = "Telephone Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5320", AccountName = "Internet Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5330", AccountName = "Food and Gas", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false, Description = "Account for the school food and kitchen gas" },
                new() { AccountNumber = "5340", AccountName = "Transport and Fuel", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false, Description = "School transport costs" },
                new() { AccountNumber = "5350", AccountName = "Student Welfare Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false, Description = "Student hygiene and immediate well being supplies" },
                new() { AccountNumber = "5360", AccountName = "Public Relations and Community Outreach", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false },
                new() { AccountNumber = "5370", AccountName = "Teachers Relief Allowance", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false },
                new() { AccountNumber = "5380", AccountName = "Cleaning and Janitorial Supplies", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false },
                new() { AccountNumber = "5390", AccountName = "Stationery Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false },
                new() { AccountNumber = "5400", AccountName = "Depreciation Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5410", AccountName = "Amortization Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5420", AccountName = "Sports Fees", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = false },
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

            foreach (var account in accounts)
            {
                if (!await context.ChartOfAccounts.AnyAsync(a => a.AccountNumber == account.AccountNumber))
                {
                    await context.ChartOfAccounts.AddAsync(account);
                }
            }
        }

        private async Task EnsureEssentialExpenseAccountsExist(AppDbContext context)
        {
            var expenseAccounts = new List<ChartOfAccount>
            {
                new() { AccountNumber = "5910", AccountName = "Bank Service Charges", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5920", AccountName = "Travel Expense", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5930", AccountName = "Meals and Entertainment", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5940", AccountName = "Dues and Subscriptions", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true },
                new() { AccountNumber = "5950", AccountName = "Consulting Fees", AccountType = "EXPENSE", AccountSubtype = "OPERATING_EXPENSE", NormalBalance = "DEBIT", IsSystemAccount = true }
            };

            foreach (var account in expenseAccounts)
            {
                if (!await context.ChartOfAccounts.AnyAsync(a => a.AccountNumber == account.AccountNumber))
                {
                    await context.ChartOfAccounts.AddAsync(account);
                    ReportProgress($"Added missing account: {account.AccountName}");
                }
            }
            await context.SaveChangesAsync();
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
            var existingCodes = await context.Currencies.Select(c => c.CurrencyCode).ToListAsync();

            var currencies = new List<Currency>
            {
                new() { CurrencyCode = "USD", CurrencyName = "US Dollar", Symbol = "$", IsBaseCurrency = true },
                new() { CurrencyCode = "EUR", CurrencyName = "Euro", Symbol = "€", IsBaseCurrency = false },
                new() { CurrencyCode = "GBP", CurrencyName = "British Pound", Symbol = "£", IsBaseCurrency = false },
                new() { CurrencyCode = "JPY", CurrencyName = "Japanese Yen", Symbol = "¥", IsBaseCurrency = false },
                new() { CurrencyCode = "CAD", CurrencyName = "Canadian Dollar", Symbol = "C$", IsBaseCurrency = false },
                new() { CurrencyCode = "ZIG", CurrencyName = "Zimbabwe Gold", Symbol = "ZiG", IsBaseCurrency = false },
                new() { CurrencyCode = "ZAR", CurrencyName = "South African Rand", Symbol = "R", IsBaseCurrency = false },
                new() { CurrencyCode = "BWP", CurrencyName = "Botswana Pula", Symbol = "P", IsBaseCurrency = false }
            };

            foreach (var currency in currencies)
            {
                if (!existingCodes.Contains(currency.CurrencyCode))
                {
                    await context.Currencies.AddAsync(currency);
                }
            }
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

        private async Task PopulateStudentGradesAsync(AppDbContext context)
        {
            if (await context.StudentGrades.AnyAsync()) return;

            var grades = new List<string>
            {
                "Pre-K", "Kindergarten", "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6",
                "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12", "Form 1",
                "Form 2", "Form 3", "Form 4", "Form 5", "Form 6",
                "Undergraduate", "Graduate", "Postgraduate"
            };

            var studentGrades = grades.Select((name, index) => new StudentGrade
            {
                GradeName = name,
                SortOrder = index + 1,
                IsActive = true
            }).ToList();

            await context.StudentGrades.AddRangeAsync(studentGrades);
        }

        private async Task CreateAccountingTriggersAsync(AppDbContext context)
        {
            // Note: Accounting balance updates have been transitioned from database triggers 
            // to service-level logic in JournalServices.cs for better status control (Posted vs Draft).
            // This method is now obsolete but kept as a placeholder if other triggers are needed.
            await Task.CompletedTask;
        }

        private async Task PopulateAssetCategoriesAsync(AppDbContext context)
        {
            // Fetch default GL account IDs from the already-seeded chart of accounts
            var accumDepnAccount = await context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == "1500");
            var depnExpenseAccount = await context.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.AccountNumber == "5400");

            var categories = new[]
            {
                new { Name = "Buildings",           AccountNo = "1420", Life = 40m, Method = "STRAIGHT_LINE" },
                new { Name = "Vehicles",             AccountNo = "1440", Life = 5m,  Method = "REDUCING_BALANCE" },
                new { Name = "Computer Equipment",   AccountNo = "1430", Life = 3m,  Method = "REDUCING_BALANCE" },
                new { Name = "Office Furniture",     AccountNo = "1450", Life = 10m, Method = "STRAIGHT_LINE" },
                new { Name = "Machinery",            AccountNo = "1430", Life = 10m, Method = "STRAIGHT_LINE" },
                new { Name = "Other Equipment",      AccountNo = "1430", Life = 5m,  Method = "STRAIGHT_LINE" },
            };

            foreach (var cat in categories)
            {
                if (!await context.AssetCategories.AnyAsync(c => c.CategoryName == cat.Name))
                {
                    var assetAccount = await context.ChartOfAccounts
                        .FirstOrDefaultAsync(a => a.AccountNumber == cat.AccountNo);

                    await context.AssetCategories.AddAsync(new PrimeAppBooks.Models.Pages.TransactionsModels.AssetCategory
                    {
                        CategoryName = cat.Name,
                        DefaultUsefulLifeYears = cat.Life,
                        DefaultDepreciationMethod = cat.Method,
                        DefaultAssetAccountId = assetAccount?.AccountId,
                        DefaultAccumDepnAccountId = accumDepnAccount?.AccountId,
                        DefaultDepnExpenseAccountId = depnExpenseAccount?.AccountId,
                        IsActive = true
                    });
                }
            }
        }
    }
}