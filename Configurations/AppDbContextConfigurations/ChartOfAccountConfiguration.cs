using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
    {
        public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
        {
            builder.ToTable("chart_of_accounts");

            builder.HasKey(a => a.AccountId);
            builder.Property(a => a.AccountId).HasColumnName("account_id");

            builder.Property(a => a.AccountNumber)
                   .HasColumnName("account_number")
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(a => a.AccountName)
                   .HasColumnName("account_name")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(a => a.AccountType)
                   .HasColumnName("account_type")
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(a => a.AccountSubtype)
                   .HasColumnName("account_subtype")
                   .HasMaxLength(50)
                   .HasDefaultValue("");

            builder.Property(a => a.Description)
                   .HasColumnName("description");

            builder.Property(a => a.ParentAccountId).HasColumnName("parent_account_id");

            builder.Property(a => a.IsActive)
                   .HasColumnName("is_active")
                   .HasDefaultValue(true)
                   .IsRequired();

            builder.Property(a => a.IsSystemAccount)
                   .HasColumnName("is_system_account")
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.Property(a => a.NormalBalance)
                   .HasColumnName("normal_balance")
                   .HasMaxLength(10)
                   .HasDefaultValue("DEBIT");

            builder.Property(a => a.OpeningBalance)
                   .HasColumnName("opening_balance")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0)
                   .IsRequired();

            builder.Property(a => a.OpeningBalanceDate)
                   .HasColumnName("opening_balance_date")
                   .HasConversion(v => v.HasValue && v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v, v => v);

            builder.Property(a => a.CurrentBalance)
                   .HasColumnName("current_balance")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0)
                   .IsRequired();

            builder.Property(a => a.CreatedBy)
                   .HasColumnName("created_by");

            builder.Property(a => a.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.Property(a => a.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            // Self-referencing relationship for parent-child accounts
            builder.HasOne(a => a.ParentAccount)
                   .WithMany(a => a.ChildAccounts)
                   .HasForeignKey(a => a.ParentAccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Foreign key relationship to JournalLines is configured in JournalLineConfiguration

            // Add indexes for performance
            builder.HasIndex(a => a.AccountNumber).HasDatabaseName("idx_chart_of_accounts_number").IsUnique();
            builder.HasIndex(a => new { a.AccountType, a.IsActive }).HasDatabaseName("idx_chart_of_accounts_type");
            builder.HasIndex(a => a.ParentAccountId).HasDatabaseName("idx_chart_of_accounts_parent");
        }
    }
}