using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformCompanyRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformCompanies",
                columns: table => new
                {
                    PlatformID = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyID = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformCompanies", x => new { x.PlatformID, x.CompanyID, x.Role });
                    table.ForeignKey(
                        name: "FK_PlatformCompanies_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformCompanies_Platforms_PlatformID",
                        column: x => x.PlatformID,
                        principalTable: "Platforms",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCompanies_CompanyID",
                table: "PlatformCompanies",
                column: "CompanyID");

            migrationBuilder.Sql("""
                                 INSERT OR IGNORE INTO "PlatformCompanies" ("PlatformID", "CompanyID", "Role")
                                 SELECT "Platforms"."ID", CAST("developer"."value" AS INTEGER), 1
                                 FROM "Platforms"
                                 CROSS JOIN json_each(
                                     CASE
                                         WHEN "Platforms"."ManufacturerIds" IS NOT NULL
                                              AND json_valid("Platforms"."ManufacturerIds")
                                             THEN "Platforms"."ManufacturerIds"
                                         ELSE '[]'
                                     END
                                 ) AS "developer"
                                 INNER JOIN "Companies" ON "Companies"."ID" = CAST("developer"."value" AS INTEGER)
                                 WHERE CAST("developer"."value" AS INTEGER) > 0;
                                 """);

            migrationBuilder.DropColumn(
                name: "ManufacturerIds",
                table: "Platforms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManufacturerIds",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                                 UPDATE "Platforms"
                                 SET "ManufacturerIds" = COALESCE(
                                     (
                                         SELECT json_group_array("CompanyID")
                                         FROM (
                                             SELECT DISTINCT "CompanyID"
                                             FROM "PlatformCompanies"
                                             WHERE "PlatformID" = "Platforms"."ID"
                                               AND "Role" = 1
                                             ORDER BY "CompanyID"
                                         )
                                     ),
                                     '[]'
                                 );
                                 """);

            migrationBuilder.DropTable(
                name: "PlatformCompanies");
        }
    }
}
