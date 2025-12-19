using Microsoft.EntityFrameworkCore;
using PrimeAppBooks.Configurations.AppDbContextConfigurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalLine> JournalLines { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
        public DbSet<JournalTemplate> JournalTemplates { get; set; }

        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<SalesInvoiceLine> SalesInvoiceLines { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}