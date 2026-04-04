using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DosyaYonetimPortali.API.Migrations
{
    /// <inheritdoc />
    public partial class QuotaAndTrashUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Files",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalStorageQuota",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "UsedStorage",
                table: "AspNetUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "TotalStorageQuota",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UsedStorage",
                table: "AspNetUsers");
        }
    }
}
