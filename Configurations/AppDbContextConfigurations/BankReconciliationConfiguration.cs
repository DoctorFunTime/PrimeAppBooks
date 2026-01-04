using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;
using System;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
    {
        public void Configure(EntityTypeBuilder<BankReconciliation> builder)
        {
            builder.ToTable("bank_reconciliations");

            builder.HasKey(r => r.ReconciliationId);
            builder.Property(r => r.ReconciliationId).HasColumnName("reconciliation_id");

            builder.Property(r => r.AccountId)
                   .HasColumnName("account_id")
                   .IsRequired();

            builder.Property(r => r.StatementDate)
                   .HasColumnName("statement_date")
                   .IsRequired()
                   .HasConversion(v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, v => v);

            builder.Property(r => r.StatementStartingBalance)
                   .HasColumnName("statement_starting_balance")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(r => r.StatementEndingBalance)
                   .HasColumnName("statement_ending_balance")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(r => r.ClearedDifference)
                   .HasColumnName("cleared_difference")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(r => r.Status)
                   .HasColumnName("status")
                   .HasMaxLength(20)
                   .HasDefaultValue("DRAFT")
                   .IsRequired();

            builder.Property(r => r.CreatedBy)
                   .HasColumnName("created_by")
                   .HasDefaultValue(1)
                   .IsRequired();

            builder.Property(r => r.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.Property(r => r.CompletedAt)
                   .HasColumnName("completed_at");

            // Relationships
            builder.HasOne(r => r.Account)
                   .WithMany()
                   .HasForeignKey(r => r.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.ReconciledLines)
                   .WithOne(l => l.BankReconciliation)
                   .HasForeignKey(l => l.ReconciliationId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
