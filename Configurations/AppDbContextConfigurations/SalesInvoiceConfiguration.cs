using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
    {
        public void Configure(EntityTypeBuilder<SalesInvoice> builder)
        {
            builder.ToTable("sales_invoices");

            builder.HasKey(s => s.SalesInvoiceId);
            builder.Property(s => s.SalesInvoiceId).HasColumnName("sales_invoice_id");

            builder.Property(s => s.InvoiceNumber)
                   .HasColumnName("invoice_number")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(s => s.CustomerId).HasColumnName("customer_id");

            builder.Property(s => s.InvoiceDate)
                   .HasColumnName("invoice_date")
                   .IsRequired();

            builder.Property(s => s.DueDate)
                   .HasColumnName("due_date")
                   .IsRequired();

            builder.Property(s => s.TotalAmount)
                   .HasColumnName("total_amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(s => s.TaxAmount)
                   .HasColumnName("tax_amount")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(s => s.DiscountAmount)
                   .HasColumnName("discount_amount")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(s => s.NetAmount)
                   .HasColumnName("net_amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(s => s.AmountReceived)
                   .HasColumnName("amount_received")
                   .HasColumnType("decimal(18,2)")
                   .HasDefaultValue(0);

            builder.Property(s => s.Balance)
                   .HasColumnName("balance")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(s => s.Status)
                   .HasColumnName("status")
                   .HasMaxLength(20)
                   .HasDefaultValue("DRAFT")
                   .IsRequired();

            builder.Property(s => s.Terms).HasColumnName("terms");
            builder.Property(s => s.Notes).HasColumnName("notes");

            builder.Property(s => s.CreatedBy).HasColumnName("created_by");

            builder.Property(s => s.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.Property(s => s.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            // Relationships
            builder.HasOne(s => s.Customer)
                   .WithMany()
                   .HasForeignKey(s => s.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Lines)
                   .WithOne(l => l.SalesInvoice)
                   .HasForeignKey(l => l.SalesInvoiceId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(s => s.InvoiceNumber).IsUnique();
            builder.HasIndex(s => s.CustomerId);
            builder.HasIndex(s => s.InvoiceDate);
            builder.HasIndex(s => s.Status);
        }
    }
}
