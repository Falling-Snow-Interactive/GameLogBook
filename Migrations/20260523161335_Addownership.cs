using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class Addownership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Ownership_Platform",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Ownership_Type",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ownership_Platform",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Ownership_Type",
                table: "Games");
        }
    }
}
