using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PrimeAppBooks.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentGradesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentGrades",
                columns: table => new
                {
                    GradeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GradeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGrades", x => x.GradeId);
                });

            migrationBuilder.InsertData(
                table: "StudentGrades",
                columns: new[] { "GradeId", "Description", "GradeName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "", "Pre-K", true, 10 },
                    { 2, "", "Kindergarten", true, 20 },
                    { 3, "", "Grade 1", true, 30 },
                    { 4, "", "Grade 2", true, 40 },
                    { 5, "", "Grade 3", true, 50 },
                    { 6, "", "Grade 4", true, 60 },
                    { 7, "", "Grade 5", true, 70 },
                    { 8, "", "Grade 6", true, 80 },
                    { 9, "", "Grade 7", true, 90 },
                    { 10, "", "Grade 8", true, 100 },
                    { 11, "", "Grade 9", true, 110 },
                    { 12, "", "Grade 10", true, 120 },
                    { 13, "", "Grade 11", true, 130 },
                    { 14, "", "Grade 12", true, 140 },
                    { 15, "", "Form 1", true, 150 },
                    { 16, "", "Form 2", true, 160 },
                    { 17, "", "Form 3", true, 170 },
                    { 18, "", "Form 4", true, 180 },
                    { 19, "", "Form 5", true, 190 },
                    { 20, "", "Form 6", true, 200 },
                    { 21, "", "Undergraduate", true, 210 },
                    { 22, "", "Graduate", true, 220 },
                    { 23, "", "Postgraduate", true, 230 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentGrades");
        }
    }
}
