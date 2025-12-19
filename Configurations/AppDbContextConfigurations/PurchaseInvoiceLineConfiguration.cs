using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class PurchaseInvoiceLineConfiguration : IEntityTypeConfiguration<PurchaseInvoiceLine>
    {
        public void Configure(EntityTypeBuilder<PurchaseInvoiceLine> builder)
        {
            builder.ToTable("purchase_invoice_lines");

            builder.HasKey(l => l.LineId);
            builder.Property(l => l.LineId).HasColumnName("line_id");

            builder.Property(l => l.PurchaseInvoiceId).HasColumnName("purchase_invoice_id");

            builder.Property(l => l.Description)
                   .HasColumnName("description")
                   .IsRequired();

            builder.Property(l => l.AccountId).HasColumnName("account_id");

            builder.Property(l => l.Quantity)
                   .HasColumnName("quantity")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(l => l.UnitPrice)
                   .HasColumnName("unit_price")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(l => l.Amount)
                   .HasColumnName("amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            // Relationships
            builder.HasOne(l => l.PurchaseInvoice)
                   .WithMany(p => p.Lines)
                   .HasForeignKey(l => l.PurchaseInvoiceId);

            builder.HasOne(l => l.Account)
                   .WithMany()
                   .HasForeignKey(l => l.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
