using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LessonResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LessonId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonResources_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonResources_LessonId",
                table: "LessonResources",
                column: "LessonId");

                        migrationBuilder.Sql(@"
                                INSERT INTO LessonResources (LessonId, Title, Url, Type, CreatedAt)
                                SELECT Id, 'Video material', VideoLink, 'video', SYSUTCDATETIME()
                                FROM Lessons
                                WHERE VideoLink IS NOT NULL AND LTRIM(RTRIM(VideoLink)) <> '';

                                INSERT INTO LessonResources (LessonId, Title, Url, Type, CreatedAt)
                                SELECT Id, 'Additional material', MaterialLink, 'material', SYSUTCDATETIME()
                                FROM Lessons
                                WHERE MaterialLink IS NOT NULL AND LTRIM(RTRIM(MaterialLink)) <> ''
                                    AND NOT EXISTS (
                                            SELECT 1
                                            FROM LessonResources lr
                                            WHERE lr.LessonId = Lessons.Id
                                                AND lr.Url = Lessons.MaterialLink
                                    );
                        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonResources");
        }
    }
}
