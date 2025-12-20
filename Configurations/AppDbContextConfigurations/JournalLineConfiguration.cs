using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
    {
        public void Configure(EntityTypeBuilder<JournalLine> builder)
        {
            builder.ToTable("journal_lines");

            builder.HasKey(l => l.LineId);
            builder.Property(l => l.LineId).HasColumnName("line_id");

            builder.Property(l => l.JournalId)
                   .HasColumnName("journal_id")
                   .IsRequired();

            builder.Property(l => l.AccountId)
                   .HasColumnName("account_id")
                   .IsRequired();

            builder.Property(l => l.LineDate)
                   .HasColumnName("line_date")
                   .IsRequired()
                   .HasConversion(v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, v => v);

            builder.Property(l => l.PeriodId).HasColumnName("period_id");

            builder.Property(l => l.DebitAmount)
                   .HasColumnName("debit_amount")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0)
                   .IsRequired();

            builder.Property(l => l.CreditAmount)
                   .HasColumnName("credit_amount")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0)
                   .IsRequired();

            builder.Property(l => l.Description)
                   .HasColumnName("description")
                   .HasDefaultValue("");

            builder.Property(l => l.Reference)
                   .HasColumnName("reference")
                   .HasMaxLength(100)
                   .HasDefaultValue("");

            builder.Property(l => l.CostCenterId).HasColumnName("cost_center_id");
            builder.Property(l => l.ProjectId).HasColumnName("project_id");

            builder.Property(l => l.CurrencyId).HasColumnName("currency_id");
            builder.Property(l => l.ExchangeRate).HasColumnName("exchange_rate").HasPrecision(18, 6).HasDefaultValue(1);
            builder.Property(l => l.ForeignDebitAmount).HasColumnName("foreign_debit_amount").HasPrecision(18, 2).HasDefaultValue(0);
            builder.Property(l => l.ForeignCreditAmount).HasColumnName("foreign_credit_amount").HasPrecision(18, 2).HasDefaultValue(0);

            builder.Property(l => l.CreatedBy)
                   .HasColumnName("created_by")
                   .HasDefaultValue(1)
                   .IsRequired();

            builder.Property(l => l.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            // FIX: Properly configure the JournalEntry relationship
            builder.HasOne(l => l.JournalEntry)
                   .WithMany(j => j.JournalLines)
                   .HasForeignKey(l => l.JournalId)
                   .OnDelete(DeleteBehavior.Cascade);

            // FIX: Configure the ChartOfAccount relationship
            builder.HasOne(l => l.ChartOfAccount)
                   .WithMany(a => a.JournalLines)
                   .HasForeignKey(l => l.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.Currency)
                    .WithMany(c => c.JournalLines)
                    .HasForeignKey(l => l.CurrencyId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(l => new { l.AccountId, l.LineDate }).HasDatabaseName("idx_journal_lines_account_date");
            builder.HasIndex(l => l.PeriodId).HasDatabaseName("idx_journal_lines_period");
            builder.HasIndex(l => l.JournalId).HasDatabaseName("idx_journal_lines_journal");
        }
    }
}