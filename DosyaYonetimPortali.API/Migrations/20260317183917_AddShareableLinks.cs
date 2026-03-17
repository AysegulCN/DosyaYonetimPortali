using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DosyaYonetimPortali.API.Migrations
{
    /// <inheritdoc />
    public partial class AddShareableLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ShareExpiration",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShareToken",
                table: "Files",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareExpiration",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "Files");
        }
    }
}
