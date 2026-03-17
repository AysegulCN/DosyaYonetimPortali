using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DosyaYonetimPortali.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStarredFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarred",
                table: "Files",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStarred",
                table: "Files");
        }
    }
}
