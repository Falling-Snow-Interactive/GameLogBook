using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class Addingmoreimagestoplatoforms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cover_ImagePath",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hero_ImagePath",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Logo_ImagePath",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Platforms",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cover_ImagePath",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Hero_ImagePath",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Logo_ImagePath",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Platforms");
        }
    }
}
