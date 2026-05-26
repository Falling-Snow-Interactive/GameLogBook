using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class AddIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImagePath",
                table: "Platforms",
                newName: "Icon_ImagePath");

            migrationBuilder.AlterColumn<string>(
                name: "Abbreviation",
                table: "Platforms",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Icon_ImagePath",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon_ImagePath",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "Icon_ImagePath",
                table: "Platforms",
                newName: "ImagePath");

            migrationBuilder.AlterColumn<string>(
                name: "Abbreviation",
                table: "Platforms",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
