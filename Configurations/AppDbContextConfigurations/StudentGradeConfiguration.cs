using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrimeAppBooks.Models;
using System;
using System.Collections.Generic;

namespace PrimeAppBooks.Configurations.AppDbContextConfigurations
{
    public class StudentGradeConfiguration : IEntityTypeConfiguration<StudentGrade>
    {
        public void Configure(EntityTypeBuilder<StudentGrade> builder)
        {
            builder.ToTable("StudentGrades");

            builder.HasKey(g => g.GradeId);

            builder.Property(g => g.GradeName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(g => g.Description)
                .HasMaxLength(255);

            builder.Property(g => g.IsActive)
                .HasDefaultValue(true);

            // Seed Data
            var grades = new List<StudentGrade>();
            int id = 1;
            int order = 10;

            var gradeNames = new[]
            {
                "Pre-K", "Kindergarten",
                "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6",
                "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12",
                "Form 1", "Form 2", "Form 3", "Form 4", "Form 5", "Form 6",
                "Undergraduate", "Graduate", "Postgraduate"
            };

            foreach (var name in gradeNames)
            {
                grades.Add(new StudentGrade
                {
                    GradeId = id++,
                    GradeName = name,
                    Description = string.Empty,
                    SortOrder = order,
                    IsActive = true
                });
                order += 10;
            }

            builder.HasData(grades);
        }
    }
}
