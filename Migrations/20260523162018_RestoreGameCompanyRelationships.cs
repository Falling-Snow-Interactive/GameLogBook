using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class RestoreGameCompanyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameCompanies",
                columns: table => new
                {
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCompanies", x => new { x.GameId, x.CompanyId, x.Role });
                    table.ForeignKey(
                        name: "FK_GameCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameCompanies_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameCompanies_CompanyId",
                table: "GameCompanies",
                column: "CompanyId");

            migrationBuilder.Sql("""
                                 INSERT OR IGNORE INTO "GameCompanies" ("GameId", "CompanyId", "Role")
                                 SELECT "Games"."Id", CAST("developer"."value" AS INTEGER), 1
                                 FROM "Games"
                                 CROSS JOIN json_each("Games"."DeveloperCompanyIds") AS "developer"
                                 INNER JOIN "Companies" ON "Companies"."Id" = CAST("developer"."value" AS INTEGER)
                                 WHERE CAST("developer"."value" AS INTEGER) > 0;
                                 """);

            migrationBuilder.Sql("""
                                 INSERT OR IGNORE INTO "GameCompanies" ("GameId", "CompanyId", "Role")
                                 SELECT "Games"."Id", CAST("publisher"."value" AS INTEGER), 2
                                 FROM "Games"
                                 CROSS JOIN json_each("Games"."PublisherCompanyIds") AS "publisher"
                                 INNER JOIN "Companies" ON "Companies"."Id" = CAST("publisher"."value" AS INTEGER)
                                 WHERE CAST("publisher"."value" AS INTEGER) > 0;
                                 """);

            migrationBuilder.DropColumn(
                name: "DeveloperCompanyIds",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PublisherCompanyIds",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeveloperCompanyIds",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "PublisherCompanyIds",
                table: "Games",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                                 UPDATE "Games"
                                 SET "DeveloperCompanyIds" = COALESCE(
                                     (
                                         SELECT json_group_array("CompanyId")
                                         FROM (
                                             SELECT DISTINCT "CompanyId"
                                             FROM "GameCompanies"
                                             WHERE "GameId" = "Games"."Id"
                                               AND "Role" = 1
                                             ORDER BY "CompanyId"
                                         )
                                     ),
                                     '[]'
                                 );
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE "Games"
                                 SET "PublisherCompanyIds" = COALESCE(
                                     (
                                         SELECT json_group_array("CompanyId")
                                         FROM (
                                             SELECT DISTINCT "CompanyId"
                                             FROM "GameCompanies"
                                             WHERE "GameId" = "Games"."Id"
                                               AND "Role" = 2
                                             ORDER BY "CompanyId"
                                         )
                                     ),
                                     '[]'
                                 );
                                 """);

            migrationBuilder.DropTable(
                name: "GameCompanies");
        }
    }
}
