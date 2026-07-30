using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.UserId);
            builder.Property(u => u.UserId).HasColumnName("user_id");

            builder.Property(u => u.Username)
                   .HasColumnName("username")
                   .HasMaxLength(100)
                   .IsRequired();
            builder.HasIndex(u => u.Username).IsUnique();

            builder.Property(u => u.PasswordHash)
                   .HasColumnName("password_hash")
                   .IsRequired();

            builder.Property(u => u.AccountName)
                   .HasColumnName("account_name")
                   .HasMaxLength(100);

            builder.Property(u => u.AccountSurname)
                   .HasColumnName("account_surname")
                   .HasMaxLength(100);

            builder.Property(u => u.AccountTitle)
                   .HasColumnName("account_title")
                   .HasMaxLength(50);

            builder.Property(u => u.AccountType)
                   .HasColumnName("accounttype")
                   .HasMaxLength(50);

            builder.Property(u => u.AccountDepartment)
                   .HasColumnName("account_department")
                   .HasMaxLength(100);

            builder.Property(u => u.AccountTasks)
                   .HasColumnName("account_tasks")
                   .HasDefaultValue(false);
        }
    }
}
