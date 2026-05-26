using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class GettingPlatformFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ownership_Capacity",
                table: "Games");

            migrationBuilder.CreateTable(
                name: "GamePlatform",
                columns: table => new
                {
                    GameID = table.Column<int>(type: "INTEGER", nullable: false),
                    PlatformID = table.Column<int>(type: "INTEGER", nullable: false),
                    Ownership = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlatform", x => new { x.GameID, x.PlatformID, x.Ownership });
                    table.ForeignKey(
                        name: "FK_GamePlatform_Games_GameID",
                        column: x => x.GameID,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GamePlatform_Platforms_PlatformID",
                        column: x => x.PlatformID,
                        principalTable: "Platforms",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_ID",
                table: "Platforms",
                column: "ID",
                unique: true,
                filter: "ID IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlatform_PlatformID",
                table: "GamePlatform",
                column: "PlatformID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamePlatform");

            migrationBuilder.DropIndex(
                name: "IX_Platforms_ID",
                table: "Platforms");

            migrationBuilder.AddColumn<int>(
                name: "Ownership_Capacity",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
