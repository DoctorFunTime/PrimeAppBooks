using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static PrimeAppBooks.Models.Pages.TransactionsModels;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class JournalTemplateConfiguration : IEntityTypeConfiguration<JournalTemplate>
    {
        public void Configure(EntityTypeBuilder<JournalTemplate> builder)
        {
            builder.ToTable("journal_templates");

            builder.HasKey(t => t.TemplateId);
            builder.Property(t => t.TemplateId).HasColumnName("template_id");

            builder.Property(t => t.Name)
                   .HasColumnName("template_name")
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(t => t.Description)
                   .HasColumnName("description")
                   .HasDefaultValue("");

            builder.Property(t => t.JournalType)
                   .HasColumnName("journal_type")
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(t => t.TemplateData)
                   .HasColumnName("template_data")
                   .HasColumnType("text")
                   .HasDefaultValue("");

            builder.Property(t => t.IsActive)
                   .HasColumnName("is_active")
                   .HasDefaultValue(true)
                   .IsRequired();

            builder.Property(t => t.CreatedBy)
                   .HasColumnName("created_by")
                   .HasDefaultValue(1)
                   .IsRequired();

            builder.Property(t => t.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            builder.Property(t => t.UpdatedAt)
                   .HasColumnName("updated_at")
                   .HasDefaultValueSql("CURRENT_TIMESTAMP")
                   .IsRequired();

            // Add indexes for performance
            builder.HasIndex(t => t.JournalType).HasDatabaseName("idx_journal_templates_type");
            builder.HasIndex(t => t.IsActive).HasDatabaseName("idx_journal_templates_active");
        }
    }
}
