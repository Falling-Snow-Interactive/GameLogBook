using System;
using GameLogBook.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GameLogBookDbContext))]
    [Migration("20260519120000_AddGameCompanyRelationships")]
    public partial class AddGameCompanyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSyncedAt",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                                 CREATE TABLE "GameCompanies_Migration" (
                                     "GameId" INTEGER NOT NULL,
                                     "CompanyId" INTEGER NOT NULL,
                                     "Role" INTEGER NOT NULL,
                                     PRIMARY KEY ("GameId", "CompanyId", "Role")
                                 );
                                 """);

            migrationBuilder.Sql("""
                                 INSERT INTO Companies (IgdbId, Name, CoverUrl, LastSyncedAt)
                                 SELECT DISTINCT NULL, trim(Games.Developer), NULL, NULL
                                 FROM Games
                                 WHERE Games.Developer IS NOT NULL
                                   AND trim(Games.Developer) <> ''
                                   AND NOT EXISTS (
                                       SELECT 1
                                       FROM Companies
                                       WHERE Companies.Name = trim(Games.Developer)
                                   );
                                 """);

            migrationBuilder.Sql("""
                                 INSERT INTO Companies (IgdbId, Name, CoverUrl, LastSyncedAt)
                                 SELECT DISTINCT NULL, trim(Games.Publisher), NULL, NULL
                                 FROM Games
                                 WHERE Games.Publisher IS NOT NULL
                                   AND trim(Games.Publisher) <> ''
                                   AND NOT EXISTS (
                                       SELECT 1
                                       FROM Companies
                                       WHERE Companies.Name = trim(Games.Publisher)
                                   );
                                 """);

            migrationBuilder.Sql("""
                                 INSERT OR IGNORE INTO GameCompanies_Migration (GameId, CompanyId, Role)
                                 SELECT Games.Id, Companies.Id, 1
                                 FROM Games
                                 INNER JOIN Companies ON Companies.Name = trim(Games.Developer)
                                 WHERE Games.Developer IS NOT NULL
                                   AND trim(Games.Developer) <> '';
                                 """);

            migrationBuilder.Sql("""
                                 INSERT OR IGNORE INTO GameCompanies_Migration (GameId, CompanyId, Role)
                                 SELECT Games.Id, Companies.Id, 2
                                 FROM Games
                                 INNER JOIN Companies ON Companies.Name = trim(Games.Publisher)
                                 WHERE Games.Publisher IS NOT NULL
                                   AND trim(Games.Publisher) <> '';
                                 """);

            migrationBuilder.Sql("""
                                 CREATE TABLE "Companies_Rebuild" (
                                     "Id" INTEGER NOT NULL CONSTRAINT "PK_Companies" PRIMARY KEY AUTOINCREMENT,
                                     "IgdbId" INTEGER NULL,
                                     "Name" TEXT NOT NULL,
                                     "CoverUrl" TEXT NULL,
                                     "LastSyncedAt" TEXT NULL
                                 );

                                 INSERT INTO "Companies_Rebuild" ("Id", "IgdbId", "Name", "CoverUrl", "LastSyncedAt")
                                 SELECT "Id", "IgdbId", "Name", "CoverUrl", "LastSyncedAt"
                                 FROM "Companies";

                                 DROP TABLE "Companies";
                                 ALTER TABLE "Companies_Rebuild" RENAME TO "Companies";
                                 """);

            migrationBuilder.Sql("""
                                 CREATE TABLE "Games_Rebuild" (
                                     "Id" INTEGER NOT NULL CONSTRAINT "PK_Games" PRIMARY KEY AUTOINCREMENT,
                                     "IgdbId" INTEGER NOT NULL,
                                     "Name" TEXT NOT NULL,
                                     "Summary" TEXT NULL,
                                     "ReleaseDate" TEXT NULL,
                                     "Cover_Url" TEXT NULL
                                 );

                                 INSERT INTO "Games_Rebuild" ("Id", "IgdbId", "Name", "Summary", "ReleaseDate", "Cover_Url")
                                 SELECT "Id", "IgdbId", "Name", "Summary", "ReleaseDate", "Cover_Url"
                                 FROM "Games";

                                 DROP TABLE "Games";
                                 ALTER TABLE "Games_Rebuild" RENAME TO "Games";
                                 """);

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

            migrationBuilder.Sql("""
                                 INSERT INTO "GameCompanies" ("GameId", "CompanyId", "Role")
                                 SELECT "GameId", "CompanyId", "Role"
                                 FROM "GameCompanies_Migration";

                                 DROP TABLE "GameCompanies_Migration";
                                 """);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IgdbId",
                table: "Companies",
                column: "IgdbId",
                unique: true,
                filter: "IgdbId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GameCompanies_CompanyId",
                table: "GameCompanies",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Companies_IgdbId",
                table: "Companies");

            migrationBuilder.AddColumn<string>(
                name: "Developer",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameIds",
                table: "Companies",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeveloper",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublisher",
                table: "Companies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                                 UPDATE Games
                                 SET Developer = (
                                     SELECT Companies.Name
                                     FROM GameCompanies
                                     INNER JOIN Companies ON Companies.Id = GameCompanies.CompanyId
                                     WHERE GameCompanies.GameId = Games.Id
                                       AND GameCompanies.Role = 1
                                     ORDER BY Companies.Name
                                     LIMIT 1
                                 );
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE Games
                                 SET Publisher = (
                                     SELECT Companies.Name
                                     FROM GameCompanies
                                     INNER JOIN Companies ON Companies.Id = GameCompanies.CompanyId
                                     WHERE GameCompanies.GameId = Games.Id
                                       AND GameCompanies.Role = 2
                                     ORDER BY Companies.Name
                                     LIMIT 1
                                 );
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE Companies
                                 SET IsDeveloper = EXISTS (
                                     SELECT 1
                                     FROM GameCompanies
                                     WHERE GameCompanies.CompanyId = Companies.Id
                                       AND GameCompanies.Role = 1
                                 );
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE Companies
                                 SET IsPublisher = EXISTS (
                                     SELECT 1
                                     FROM GameCompanies
                                     WHERE GameCompanies.CompanyId = Companies.Id
                                       AND GameCompanies.Role = 2
                                 );
                                 """);

            migrationBuilder.DropTable(
                name: "GameCompanies");

            migrationBuilder.Sql("""
                                 CREATE TABLE "Companies_Rebuild" (
                                     "Id" INTEGER NOT NULL CONSTRAINT "PK_Companies" PRIMARY KEY AUTOINCREMENT,
                                     "IgdbId" INTEGER NULL,
                                     "Name" TEXT NOT NULL,
                                     "GameIds" TEXT NOT NULL,
                                     "IsPublisher" INTEGER NOT NULL,
                                     "IsDeveloper" INTEGER NOT NULL,
                                     "CoverUrl" TEXT NULL
                                 );

                                 INSERT INTO "Companies_Rebuild" ("Id", "IgdbId", "Name", "GameIds", "IsPublisher", "IsDeveloper", "CoverUrl")
                                 SELECT "Id", "IgdbId", "Name", "GameIds", "IsPublisher", "IsDeveloper", "CoverUrl"
                                 FROM "Companies";

                                 DROP TABLE "Companies";
                                 ALTER TABLE "Companies_Rebuild" RENAME TO "Companies";
                                 """);
        }
    }
}
