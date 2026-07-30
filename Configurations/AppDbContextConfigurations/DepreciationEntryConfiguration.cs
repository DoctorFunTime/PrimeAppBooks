using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class DepreciationEntryConfiguration : IEntityTypeConfiguration<DepreciationEntry>
    {
        public void Configure(EntityTypeBuilder<DepreciationEntry> builder)
        {
            builder.ToTable("depreciation_entries");
            builder.HasKey(e => e.EntryId);
            builder.Property(e => e.EntryId).HasColumnName("entry_id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.PeriodDate).HasColumnName("period_date").IsRequired();
            builder.Property(e => e.DepreciationAmount).HasColumnName("depreciation_amount").HasColumnType("decimal(18,2)");
            builder.Property(e => e.BookValueAfter).HasColumnName("book_value_after").HasColumnType("decimal(18,2)");
            builder.Property(e => e.JournalId).HasColumnName("journal_id");
            builder.Property(e => e.Notes).HasColumnName("notes");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(e => e.Asset)
                   .WithMany(a => a.DepreciationEntries)
                   .HasForeignKey(e => e.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Journal)
                   .WithMany()
                   .HasForeignKey(e => e.JournalId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
