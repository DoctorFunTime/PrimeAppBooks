using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using PrimeAppBooks.Models;

    public class BillConfiguration : IEntityTypeConfiguration<Bill>
    {
        public void Configure(EntityTypeBuilder<Bill> builder)
        {
            builder.ToTable("bills");

            builder.HasKey(b => b.BillId);
            builder.Property(b => b.BillId).HasColumnName("bill_id");

            builder.Property(b => b.BillNumber)
                   .HasColumnName("bill_number")
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(b => b.VendorId).HasColumnName("vendor_id");

            builder.Property(b => b.BillDate)
                   .HasColumnName("bill_date")
                   .IsRequired();

            builder.Property(b => b.DueDate)
                   .HasColumnName("due_date")
                   .IsRequired();

            builder.Property(b => b.TotalAmount)
                   .HasColumnName("total_amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(b => b.TaxAmount)
                   .HasColumnName("tax_amount")
                   .HasColumnType("decimal(18,2)");

            builder.Property(b => b.DiscountAmount)
                   .HasColumnName("discount_amount")
                   .HasColumnType("decimal(18,2)");

            builder.Property(b => b.NetAmount)
                   .HasColumnName("net_amount")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(b => b.AmountPaid)
                   .HasColumnName("amount_paid")
                   .HasColumnType("decimal(18,2)");

            builder.Property(b => b.Balance)
                   .HasColumnName("balance")
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(b => b.Status)
                   .HasColumnName("status")
                   .HasMaxLength(20)
                   .HasDefaultValue("DRAFT");

            builder.Property(b => b.Terms).HasColumnName("terms");
            builder.Property(b => b.Notes).HasColumnName("notes");

            builder.Property(b => b.CreatedBy).HasColumnName("created_by");

            builder.Property(b => b.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(b => b.UpdatedAt)
                   .HasColumnName("updated_at");

            builder.HasMany(b => b.Payments)
                   .WithOne(p => p.Bill)
                   .HasForeignKey(p => p.BillId);
        }
    }
}