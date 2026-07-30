using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class AssetDisposalConfiguration : IEntityTypeConfiguration<AssetDisposal>
    {
        public void Configure(EntityTypeBuilder<AssetDisposal> builder)
        {
            builder.ToTable("asset_disposals");
            builder.HasKey(d => d.DisposalId);
            builder.Property(d => d.DisposalId).HasColumnName("disposal_id");
            builder.Property(d => d.AssetId).HasColumnName("asset_id");
            builder.Property(d => d.DisposalDate).HasColumnName("disposal_date").IsRequired();
            builder.Property(d => d.SaleProceeds).HasColumnName("sale_proceeds").HasColumnType("decimal(18,2)").HasDefaultValue(0);
            builder.Property(d => d.BookValueAtDisposal).HasColumnName("book_value_at_disposal").HasColumnType("decimal(18,2)");
            builder.Property(d => d.GainOrLoss).HasColumnName("gain_or_loss").HasColumnType("decimal(18,2)");
            builder.Property(d => d.DisposalType).HasColumnName("disposal_type").HasMaxLength(20).HasDefaultValue("SALE");
            builder.Property(d => d.ProceedsAccountId).HasColumnName("proceeds_account_id");
            builder.Property(d => d.JournalId).HasColumnName("journal_id");
            builder.Property(d => d.Notes).HasColumnName("notes");
            builder.Property(d => d.CreatedBy).HasColumnName("created_by");
            builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(d => d.Asset)
                   .WithOne(a => a.Disposal)
                   .HasForeignKey<AssetDisposal>(d => d.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Journal)
                   .WithMany()
                   .HasForeignKey(d => d.JournalId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
