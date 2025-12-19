using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimeAppBooks.Models;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("vendors");

            builder.HasKey(v => v.VendorId);
            builder.Property(v => v.VendorId).HasColumnName("vendor_id");

            builder.Property(v => v.VendorName)
                   .HasColumnName("vendor_name")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(v => v.VendorCode)
                   .HasColumnName("vendor_code")
                   .HasMaxLength(50);

            builder.Property(v => v.ContactPerson)
                   .HasColumnName("contact_person")
                   .HasMaxLength(255);

            builder.Property(v => v.Email)
                   .HasColumnName("email")
                   .HasMaxLength(255);

            builder.Property(v => v.Phone)
                   .HasColumnName("phone")
                   .HasMaxLength(50);

            builder.Property(v => v.Address).HasColumnName("address");
            builder.Property(v => v.TaxId).HasColumnName("tax_id").HasMaxLength(50);
            
            builder.Property(v => v.DefaultExpenseAccountId).HasColumnName("default_expense_account_id");
            builder.Property(v => v.DefaultPaymentTermsId).HasColumnName("default_payment_terms_id");
            
            builder.Property(v => v.Notes).HasColumnName("notes");
            builder.Property(v => v.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            builder.Property(v => v.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(v => v.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
