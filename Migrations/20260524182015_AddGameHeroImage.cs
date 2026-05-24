using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class AddGameHeroImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Games",
                newName: "ID");

            migrationBuilder.AlterColumn<long>(
                name: "IgdbId",
                table: "Games",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Hero_ImagePath",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hero_ImagePath",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Games",
                newName: "Id");

            migrationBuilder.AlterColumn<long>(
                name: "IgdbId",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
