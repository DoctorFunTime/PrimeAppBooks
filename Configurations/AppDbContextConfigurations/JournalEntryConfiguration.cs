using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
    {
        public void Configure(EntityTypeBuilder<JournalEntry> builder)
        {
            builder.ToTable("journal_entries");

            builder.HasKey(j => j.JournalId);
            builder.Property(j => j.JournalId).HasColumnName("journal_id");

            builder.Property(j => j.JournalNumber)
                   .HasColumnName("journal_number")
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(j => j.JournalDate)
                   .HasColumnName("journal_date")
                   .IsRequired()
                   .HasConversion(v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v, v => v);

            builder.Property(j => j.PeriodId).HasColumnName("period_id");

            builder.Property(j => j.Reference)
                   .HasColumnName("reference")
                   .HasMaxLength(100)
                   .HasDefaultValue("");

            builder.Property(j => j.Description)
                   .HasColumnName("description")
                   .IsRequired();

            builder.Property(j => j.JournalType)
                   .HasColumnName("journal_type")
                   .HasMaxLength(50)
                   .IsRequired()
                   .HasDefaultValue("GENERAL");

            builder.Property(j => j.Amount)
                   .HasColumnName("amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(j => j.Status)
                   .HasColumnName("status")
                   .HasMaxLength(20)
                   .HasDefaultValue("DRAFT")
                   .IsRequired();

            builder.Property(j => j.PostedBy).HasColumnName("posted_by");
            builder.Property(j => j.PostedAt)
                   .HasColumnName("posted_at")
                   .HasConversion(v => v.HasValue && v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v, v => v);

            builder.Property(j => j.CreatedBy)
                   .HasColumnName("created_by")
                   .HasDefaultValue(1)
                   .IsRequired();

            builder.Property(j => j.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.Property(j => j.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.HasMany(j => j.JournalLines)
                   .WithOne(l => l.JournalEntry)
                   .HasForeignKey(l => l.JournalId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Add indexes for performance
            builder.HasIndex(j => j.JournalDate).HasDatabaseName("idx_journal_entries_date");
            builder.HasIndex(j => j.PeriodId).HasDatabaseName("idx_journal_entries_period");
            builder.HasIndex(j => j.Status).HasDatabaseName("idx_journal_entries_status");
            builder.HasIndex(j => j.JournalNumber).HasDatabaseName("idx_journal_entries_number").IsUnique();
        }
    }
}
