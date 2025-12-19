using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimeAppBooks.Models;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> builder)
        {
            builder.ToTable("payment_methods");
            builder.HasKey(pm => pm.MethodId);
            builder.Property(pm => pm.MethodId).HasColumnName("method_id");
            builder.Property(pm => pm.MethodName).HasColumnName("method_name").HasMaxLength(50).IsRequired();
            builder.Property(pm => pm.MethodCode).HasColumnName("method_code").HasMaxLength(50).IsRequired();
            builder.Property(pm => pm.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        }
    }

    public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
    {
        public void Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("currencies");
            builder.HasKey(c => c.CurrencyId);
            builder.Property(c => c.CurrencyId).HasColumnName("currency_id");
            builder.Property(c => c.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            builder.Property(c => c.CurrencyName).HasColumnName("currency_name").HasMaxLength(50).IsRequired();
            builder.Property(c => c.Symbol).HasColumnName("symbol").HasMaxLength(5);
            builder.Property(c => c.IsBaseCurrency).HasColumnName("is_base_currency");
        }
    }

    public class TaxRateConfiguration : IEntityTypeConfiguration<TaxRate>
    {
        public void Configure(EntityTypeBuilder<TaxRate> builder)
        {
            builder.ToTable("tax_rates");
            builder.HasKey(t => t.TaxRateId);
            builder.Property(t => t.TaxRateId).HasColumnName("tax_rate_id");
            builder.Property(t => t.TaxName).HasColumnName("tax_name").HasMaxLength(100).IsRequired();
            builder.Property(t => t.TaxCode).HasColumnName("tax_code").HasMaxLength(50).IsRequired();
            builder.Property(t => t.Rate).HasColumnName("rate").HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(t => t.TaxType).HasColumnName("tax_type").HasMaxLength(20).IsRequired();
            builder.Property(t => t.EffectiveFrom).HasColumnName("effective_from");
            builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        }
    }

    public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
    {
        public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
        {
            builder.ToTable("accounting_periods");
            builder.HasKey(p => p.PeriodId);
            builder.Property(p => p.PeriodId).HasColumnName("period_id");
            builder.Property(p => p.PeriodName).HasColumnName("period_name").HasMaxLength(50).IsRequired();
            builder.Property(p => p.StartDate).HasColumnName("start_date").IsRequired();
            builder.Property(p => p.EndDate).HasColumnName("end_date").IsRequired();
            builder.Property(p => p.FiscalYear).HasColumnName("fiscal_year");
            builder.Property(p => p.PeriodNumber).HasColumnName("period_number");
            builder.Property(p => p.IsClosed).HasColumnName("is_closed").HasDefaultValue(false);
            builder.Property(p => p.ClosedAt).HasColumnName("closed_at");
        }
    }

    public class AccountingSettingConfiguration : IEntityTypeConfiguration<AccountingSetting>
    {
        public void Configure(EntityTypeBuilder<AccountingSetting> builder)
        {
            builder.ToTable("accounting_settings");
            builder.HasKey(s => s.SettingId);
            builder.Property(s => s.SettingId).HasColumnName("setting_id");
            builder.Property(s => s.SettingKey).HasColumnName("setting_key").HasMaxLength(100).IsRequired();
            builder.Property(s => s.SettingValue).HasColumnName("setting_value").IsRequired();
            builder.Property(s => s.Description).HasColumnName("description");
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
