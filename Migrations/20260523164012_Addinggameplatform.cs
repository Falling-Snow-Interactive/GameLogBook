using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class Addinggameplatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ownership_Platform",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "Ownership_Type",
                table: "Games",
                newName: "Ownership_Capacity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ownership_Capacity",
                table: "Games",
                newName: "Ownership_Type");

            migrationBuilder.AddColumn<int>(
                name: "Ownership_Platform",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
