using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
        {
            builder.ToTable("purchase_invoices");

            builder.HasKey(p => p.PurchaseInvoiceId);
            builder.Property(p => p.PurchaseInvoiceId).HasColumnName("purchase_invoice_id");

            builder.Property(p => p.InvoiceNumber)
                   .HasColumnName("invoice_number")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(p => p.VendorId).HasColumnName("vendor_id");

            builder.Property(p => p.InvoiceDate)
                   .HasColumnName("invoice_date")
                   .IsRequired();

            builder.Property(p => p.DueDate)
                   .HasColumnName("due_date")
                   .IsRequired();

            builder.Property(p => p.TotalAmount)
                   .HasColumnName("total_amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.TaxAmount)
                   .HasColumnName("tax_amount")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(p => p.DiscountAmount)
                   .HasColumnName("discount_amount")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(p => p.NetAmount)
                   .HasColumnName("net_amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.AmountPaid)
                   .HasColumnName("amount_paid")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(p => p.Balance)
                   .HasColumnName("balance")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.Status)
                   .HasColumnName("status")
                   .HasMaxLength(20)
                   .HasDefaultValue("DRAFT")
                   .IsRequired();

            builder.Property(p => p.Terms).HasColumnName("terms");
            builder.Property(p => p.Notes).HasColumnName("notes");

            builder.Property(p => p.CurrencyId).HasColumnName("currency_id");
            builder.Property(p => p.ExchangeRate).HasColumnName("exchange_rate").HasPrecision(18, 6).HasDefaultValue(1);

            builder.Property(p => p.CreatedBy).HasColumnName("created_by");

            builder.Property(p => p.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.Property(p => p.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            // Relationships
            builder.HasOne(p => p.Vendor)
                   .WithMany()
                   .HasForeignKey(p => p.VendorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(p => p.Lines)
                   .WithOne(l => l.PurchaseInvoice)
                   .HasForeignKey(l => l.PurchaseInvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(p => p.InvoiceNumber);
            builder.HasIndex(p => p.VendorId);
            builder.HasIndex(p => p.InvoiceDate);
            builder.HasIndex(p => p.Status);
        }
    }
}
