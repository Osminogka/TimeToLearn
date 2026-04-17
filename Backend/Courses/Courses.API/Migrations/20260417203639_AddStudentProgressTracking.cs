using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentCourseGrades",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    CourseId = table.Column<long>(type: "bigint", nullable: false),
                    Mark = table.Column<int>(type: "int", nullable: false),
                    GivenByTeacherId = table.Column<long>(type: "bigint", nullable: false),
                    GivenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCourseGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCourseGrades_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentLessonCompletions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    LessonId = table.Column<long>(type: "bigint", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLessonCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentLessonCompletions_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseGrades_CourseId",
                table: "StudentCourseGrades",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseGrades_StudentId",
                table: "StudentCourseGrades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourseGrades_StudentId_CourseId",
                table: "StudentCourseGrades",
                columns: new[] { "StudentId", "CourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonCompletions_LessonId",
                table: "StudentLessonCompletions",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonCompletions_StudentId",
                table: "StudentLessonCompletions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonCompletions_StudentId_LessonId",
                table: "StudentLessonCompletions",
                columns: new[] { "StudentId", "LessonId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentCourseGrades");

            migrationBuilder.DropTable(
                name: "StudentLessonCompletions");
        }
    }
}
