using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimeAppBooks.Models;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.PaymentId);
        builder.Property(p => p.PaymentId).HasColumnName("payment_id");

        builder.Property(p => p.PaymentNumber)
               .HasColumnName("payment_number")
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(p => p.PaymentDate)
               .HasColumnName("payment_date")
               .IsRequired();

        builder.Property(p => p.VendorId).HasColumnName("vendor_id");
        builder.Property(p => p.BillId).HasColumnName("bill_id");

        builder.Property(p => p.PaymentMethod)
               .HasColumnName("payment_method")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(p => p.Amount)
               .HasColumnName("amount")
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(p => p.ReferenceNumber)
               .HasColumnName("reference_number")
               .HasMaxLength(100);

        builder.Property(p => p.Memo).HasColumnName("memo");

        builder.Property(p => p.Status)
               .HasColumnName("status")
               .HasMaxLength(20)
               .HasDefaultValue("PENDING");

        builder.Property(p => p.BankAccountId).HasColumnName("bank_account_id");
        builder.Property(p => p.ProcessedBy).HasColumnName("processed_by");
        builder.Property(p => p.ProcessedAt).HasColumnName("processed_at");

        builder.Property(p => p.CreatedBy).HasColumnName("created_by");

        builder.Property(p => p.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(p => p.Bill)
               .WithMany(b => b.Payments)
               .HasForeignKey(p => p.BillId);
    }
}