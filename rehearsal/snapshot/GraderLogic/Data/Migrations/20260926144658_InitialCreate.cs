using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraderLogic.Data.Migrations;

/// <inheritdoc />
public partial class _20260926144658_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Courses",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courses", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SchoolYears",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolYears", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SchoolClasses",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                SchoolYearId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolClasses", x => x.Id);
                table.ForeignKey(
                    name: "FK_SchoolClasses_SchoolYears_SchoolYearId",
                    column: x => x.SchoolYearId,
                    principalTable: "SchoolYears",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Exams",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ExamDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                SchoolClassId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Exams", x => x.Id);
                table.ForeignKey(
                    name: "FK_Exams_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Exams_SchoolClasses_SchoolClassId",
                    column: x => x.SchoolClassId,
                    principalTable: "SchoolClasses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Students",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                LastName = table.Column<string>(type: "TEXT", maxLength: 75, nullable: false),
                SchoolClassId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Students", x => x.Id);
                table.ForeignKey(
                    name: "FK_Students_SchoolClasses_SchoolClassId",
                    column: x => x.SchoolClassId,
                    principalTable: "SchoolClasses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "ExamResults",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Percentage = table.Column<double>(type: "REAL", nullable: false),
                ExamId = table.Column<int>(type: "INTEGER", nullable: false),
                StudentId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExamResults", x => x.Id);
                table.CheckConstraint("CK_ExamResults_Percentage", "\"Percentage\" BETWEEN 0 AND 100");
                table.ForeignKey(
                    name: "FK_ExamResults_Exams_ExamId",
                    column: x => x.ExamId,
                    principalTable: "Exams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExamResults_Students_StudentId",
                    column: x => x.StudentId,
                    principalTable: "Students",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Courses_Name",
            table: "Courses",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExamResults_ExamId_StudentId",
            table: "ExamResults",
            columns: new[] { "ExamId", "StudentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExamResults_StudentId",
            table: "ExamResults",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_Exams_CourseId_SchoolClassId_ExamDate",
            table: "Exams",
            columns: new[] { "CourseId", "SchoolClassId", "ExamDate" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Exams_SchoolClassId",
            table: "Exams",
            column: "SchoolClassId");

        migrationBuilder.CreateIndex(
            name: "IX_SchoolClasses_SchoolYearId_Name",
            table: "SchoolClasses",
            columns: new[] { "SchoolYearId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SchoolYears_Name",
            table: "SchoolYears",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Students_SchoolClassId",
            table: "Students",
            column: "SchoolClassId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ExamResults");

        migrationBuilder.DropTable(
            name: "Exams");

        migrationBuilder.DropTable(
            name: "Students");

        migrationBuilder.DropTable(
            name: "Courses");

        migrationBuilder.DropTable(
            name: "SchoolClasses");

        migrationBuilder.DropTable(
            name: "SchoolYears");
    }
}
