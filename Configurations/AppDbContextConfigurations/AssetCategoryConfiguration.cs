using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
    {
        public void Configure(EntityTypeBuilder<AssetCategory> builder)
        {
            builder.ToTable("asset_categories");
            builder.HasKey(c => c.CategoryId);
            builder.Property(c => c.CategoryId).HasColumnName("category_id");
            builder.Property(c => c.CategoryName).HasColumnName("category_name").HasMaxLength(150).IsRequired();
            builder.Property(c => c.Description).HasColumnName("description");
            builder.Property(c => c.DefaultUsefulLifeYears).HasColumnName("default_useful_life_years").HasColumnType("decimal(5,2)");
            builder.Property(c => c.DefaultDepreciationMethod).HasColumnName("default_depreciation_method").HasMaxLength(30).HasDefaultValue("STRAIGHT_LINE");
            builder.Property(c => c.DefaultAssetAccountId).HasColumnName("default_asset_account_id");
            builder.Property(c => c.DefaultAccumDepnAccountId).HasColumnName("default_accum_depn_account_id");
            builder.Property(c => c.DefaultDepnExpenseAccountId).HasColumnName("default_depn_expense_account_id");
            builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            builder.HasMany(c => c.Assets)
                   .WithOne(a => a.Category)
                   .HasForeignKey(a => a.CategoryId);
        }
    }
}
