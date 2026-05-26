using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class updatetogameplatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GameIds",
                table: "Platforms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameIds",
                table: "Platforms",
                type: "TEXT",
                nullable: true);
        }
    }
}
