using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class AddGameLogoImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Logo_ImagePath",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Logo_ImagePath",
                table: "Games");
        }
    }
}
