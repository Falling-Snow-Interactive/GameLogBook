using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationalDbs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameCompanies_Companies_CompanyId",
                table: "GameCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_GameCompanies_Games_GameId",
                table: "GameCompanies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IgdbId",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "GameCompanies",
                newName: "CompanyID");

            migrationBuilder.RenameColumn(
                name: "GameId",
                table: "GameCompanies",
                newName: "GameID");

            migrationBuilder.RenameIndex(
                name: "IX_GameCompanies_CompanyId",
                table: "GameCompanies",
                newName: "IX_GameCompanies_CompanyID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Companies",
                newName: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ID",
                table: "Companies",
                column: "ID",
                unique: true,
                filter: "ID IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_GameCompanies_Companies_CompanyID",
                table: "GameCompanies",
                column: "CompanyID",
                principalTable: "Companies",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GameCompanies_Games_GameID",
                table: "GameCompanies",
                column: "GameID",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameCompanies_Companies_CompanyID",
                table: "GameCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_GameCompanies_Games_GameID",
                table: "GameCompanies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ID",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "CompanyID",
                table: "GameCompanies",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "GameID",
                table: "GameCompanies",
                newName: "GameId");

            migrationBuilder.RenameIndex(
                name: "IX_GameCompanies_CompanyID",
                table: "GameCompanies",
                newName: "IX_GameCompanies_CompanyId");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "Companies",
                newName: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IgdbId",
                table: "Companies",
                column: "IgdbId",
                unique: true,
                filter: "IgdbId IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_GameCompanies_Companies_CompanyId",
                table: "GameCompanies",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GameCompanies_Games_GameId",
                table: "GameCompanies",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
