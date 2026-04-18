using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Courses.API.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizAttemptPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttemptPolicy",
                table: "QuizQuestions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "reattempt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptPolicy",
                table: "QuizQuestions");
        }
    }
}
