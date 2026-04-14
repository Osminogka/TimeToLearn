using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInviteAsTeacherToEntryRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InviteAsTeacher",
                table: "EntryRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteAsTeacher",
                table: "EntryRequests");
        }
    }
}
