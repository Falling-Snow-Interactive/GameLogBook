using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPlatformDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameIds",
                table: "Platforms",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<long>(
                name: "IgdbId",
                table: "Platforms",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReleaseDate",
                table: "Platforms",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameIds",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "IgdbId",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "Platforms");
        }
    }
}
