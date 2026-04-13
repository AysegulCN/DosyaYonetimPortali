using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DosyaYonetimPortali.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFileShareSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Files",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Files",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "FileShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CanEdit = table.Column<bool>(type: "bit", nullable: false),
                    SharedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileShares_AspNetUsers_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileShares_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileShares_FileId",
                table: "FileShares",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileShares_SharedWithUserId",
                table: "FileShares",
                column: "SharedWithUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileShares");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Files");
        }
    }
}
