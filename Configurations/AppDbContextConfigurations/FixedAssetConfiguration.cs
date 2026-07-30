using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
    {
        public void Configure(EntityTypeBuilder<FixedAsset> builder)
        {
            builder.ToTable("fixed_assets");
            builder.HasKey(a => a.AssetId);
            builder.Property(a => a.AssetId).HasColumnName("asset_id");
            builder.Property(a => a.AssetCode).HasColumnName("asset_code").HasMaxLength(50).IsRequired();
            builder.Property(a => a.AssetName).HasColumnName("asset_name").HasMaxLength(200).IsRequired();
            builder.Property(a => a.Description).HasColumnName("description");
            builder.Property(a => a.CategoryId).HasColumnName("category_id");
            builder.Property(a => a.AssetAccountId).HasColumnName("asset_account_id");
            builder.Property(a => a.AccumDepnAccountId).HasColumnName("accum_depn_account_id");
            builder.Property(a => a.DepnExpenseAccountId).HasColumnName("depn_expense_account_id");
            builder.Property(a => a.CwipAccountId).HasColumnName("cwip_account_id").IsRequired(false);
            builder.Property(a => a.PurchaseDate).HasColumnName("purchase_date").IsRequired();
            builder.Property(a => a.PurchaseCost).HasColumnName("purchase_cost").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(a => a.ResidualValue).HasColumnName("residual_value").HasColumnType("decimal(18,2)").HasDefaultValue(0);
            builder.Property(a => a.UsefulLifeYears).HasColumnName("useful_life_years").HasColumnType("decimal(5,2)");
            builder.Property(a => a.DepreciationMethod).HasColumnName("depreciation_method").HasMaxLength(30).HasDefaultValue("STRAIGHT_LINE");
            builder.Property(a => a.AccumulatedDepreciation).HasColumnName("accumulated_depreciation").HasColumnType("decimal(18,2)").HasDefaultValue(0);
            builder.Property(a => a.BookValue).HasColumnName("book_value").HasColumnType("decimal(18,2)");
            builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(25).HasDefaultValue("ACTIVE");
            builder.Property(a => a.Notes).HasColumnName("notes");
            builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            builder.Property(a => a.CreatedBy).HasColumnName("created_by");
            builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(a => a.UpdatedAt).HasColumnName("updated_at");

            // Unique asset code
            builder.HasIndex(a => a.AssetCode).IsUnique();

            // FK: Category
            builder.HasOne(a => a.Category)
                   .WithMany(c => c.Assets)
                   .HasForeignKey(a => a.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            // FK: Asset GL Account (no cascade - accounts must remain)
            builder.HasOne(a => a.AssetAccount)
                   .WithMany()
                   .HasForeignKey(a => a.AssetAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.AccumDepnAccount)
                   .WithMany()
                   .HasForeignKey(a => a.AccumDepnAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.DepnExpenseAccount)
                   .WithMany()
                   .HasForeignKey(a => a.DepnExpenseAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.CwipAccount)
                   .WithMany()
                   .HasForeignKey(a => a.CwipAccountId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(a => a.DepreciationEntries)
                   .WithOne(e => e.Asset)
                   .HasForeignKey(e => e.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Disposal)
                   .WithOne(d => d.Asset)
                   .HasForeignKey<AssetDisposal>(d => d.AssetId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
