using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class ChangeEntityMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "StoredFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "ChangedById",
                table: "StoredFiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedById",
                table: "StoredFiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_CreatedById",
                table: "StoredFiles",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_StoredFiles_AspNetUsers_CreatedById",
                table: "StoredFiles",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoredFiles_AspNetUsers_CreatedById",
                table: "StoredFiles");

            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_CreatedById",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "ChangedById",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "StoredFiles");

            migrationBuilder.AddColumn<Guid>(
                name: "AppUserId",
                table: "StoredFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
