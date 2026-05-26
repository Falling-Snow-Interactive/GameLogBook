using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class AddingILibraryEntrytocompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgdbId",
                table: "Games",
                newName: "IGDB");

            migrationBuilder.RenameColumn(
                name: "IgdbId",
                table: "Companies",
                newName: "IGDB");

            migrationBuilder.AddColumn<string>(
                name: "Cover_ImagePath",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FoundedDate",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hero_ImagePath",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon_ImagePath",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo_ImagePath",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Companies",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cover_ImagePath",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FoundedDate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Hero_ImagePath",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Icon_ImagePath",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Logo_ImagePath",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "IGDB",
                table: "Games",
                newName: "IgdbId");

            migrationBuilder.RenameColumn(
                name: "IGDB",
                table: "Companies",
                newName: "IgdbId");
        }
    }
}
