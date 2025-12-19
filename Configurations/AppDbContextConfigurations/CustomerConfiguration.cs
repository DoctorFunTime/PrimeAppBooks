using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimeAppBooks.Models;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers");

            builder.HasKey(c => c.CustomerId);
            builder.Property(c => c.CustomerId).HasColumnName("customer_id");

            builder.Property(c => c.CustomerName)
                   .HasColumnName("customer_name")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(c => c.CustomerCode)
                   .HasColumnName("customer_code")
                   .HasMaxLength(50);

            builder.Property(c => c.ContactPerson)
                   .HasColumnName("contact_person")
                   .HasMaxLength(255);

            builder.Property(c => c.Email)
                   .HasColumnName("email")
                   .HasMaxLength(255);

            builder.Property(c => c.Phone)
                   .HasColumnName("phone")
                   .HasMaxLength(50);

            builder.Property(c => c.BillingAddress).HasColumnName("billing_address");
            builder.Property(c => c.ShippingAddress).HasColumnName("shipping_address");
            builder.Property(c => c.TaxId).HasColumnName("tax_id").HasMaxLength(50);
            
            builder.Property(c => c.DefaultRevenueAccountId).HasColumnName("default_revenue_account_id");
            builder.Property(c => c.DefaultPaymentTermsId).HasColumnName("default_payment_terms_id");
            
            builder.Property(c => c.Notes).HasColumnName("notes");
            builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);

            builder.Property(c => c.IsAutoInvoiceEnabled).HasColumnName("is_auto_invoice_enabled").HasDefaultValue(false);
            builder.Property(c => c.AutoInvoiceFrequency).HasColumnName("auto_invoice_frequency").HasMaxLength(50);
            builder.Property(c => c.AutoInvoiceInterval).HasColumnName("auto_invoice_interval").HasDefaultValue(1);
            builder.Property(c => c.AutoInvoiceAmount).HasColumnName("auto_invoice_amount").HasPrecision(18, 2);
            builder.Property(c => c.NextAutoInvoiceDate).HasColumnName("next_auto_invoice_date");

            builder.Property(c => c.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(c => c.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
