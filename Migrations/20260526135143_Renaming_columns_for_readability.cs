using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class Renaming_columns_for_readability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Logo_ImagePath",
                table: "Platforms",
                newName: "ShortName");

            migrationBuilder.RenameColumn(
                name: "IgdbId",
                table: "Platforms",
                newName: "IGDB");

            migrationBuilder.RenameColumn(
                name: "Icon_ImagePath",
                table: "Platforms",
                newName: "Logo_Path");

            migrationBuilder.RenameColumn(
                name: "Hero_ImagePath",
                table: "Platforms",
                newName: "Icon_Path");

            migrationBuilder.RenameColumn(
                name: "Cover_ImagePath",
                table: "Platforms",
                newName: "Hero_Path");

            migrationBuilder.RenameColumn(
                name: "Abbreviation",
                table: "Platforms",
                newName: "Cover_Path");

            migrationBuilder.RenameColumn(
                name: "Logo_ImagePath",
                table: "Games",
                newName: "Logo_Path");

            migrationBuilder.RenameColumn(
                name: "Icon_ImagePath",
                table: "Games",
                newName: "Icon_Path");

            migrationBuilder.RenameColumn(
                name: "Hero_ImagePath",
                table: "Games",
                newName: "Hero_Path");

            migrationBuilder.RenameColumn(
                name: "Cover_ImagePath",
                table: "Games",
                newName: "Cover_Path");

            migrationBuilder.RenameColumn(
                name: "Logo_ImagePath",
                table: "Companies",
                newName: "Logo_Path");

            migrationBuilder.RenameColumn(
                name: "Icon_ImagePath",
                table: "Companies",
                newName: "Icon_Path");

            migrationBuilder.RenameColumn(
                name: "Hero_ImagePath",
                table: "Companies",
                newName: "Hero_Path");

            migrationBuilder.RenameColumn(
                name: "Cover_ImagePath",
                table: "Companies",
                newName: "Cover_Path");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShortName",
                table: "Platforms",
                newName: "Logo_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Logo_Path",
                table: "Platforms",
                newName: "Icon_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Icon_Path",
                table: "Platforms",
                newName: "Hero_ImagePath");

            migrationBuilder.RenameColumn(
                name: "IGDB",
                table: "Platforms",
                newName: "IgdbId");

            migrationBuilder.RenameColumn(
                name: "Hero_Path",
                table: "Platforms",
                newName: "Cover_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Cover_Path",
                table: "Platforms",
                newName: "Abbreviation");

            migrationBuilder.RenameColumn(
                name: "Logo_Path",
                table: "Games",
                newName: "Logo_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Icon_Path",
                table: "Games",
                newName: "Icon_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Hero_Path",
                table: "Games",
                newName: "Hero_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Cover_Path",
                table: "Games",
                newName: "Cover_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Logo_Path",
                table: "Companies",
                newName: "Logo_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Icon_Path",
                table: "Companies",
                newName: "Icon_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Hero_Path",
                table: "Companies",
                newName: "Hero_ImagePath");

            migrationBuilder.RenameColumn(
                name: "Cover_Path",
                table: "Companies",
                newName: "Cover_ImagePath");
        }
    }
}
