using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class UseManufacturerCompanyIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManufacturerIds",
                table: "Platforms",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql(
                """
                UPDATE "Platforms"
                SET "ManufacturerIds" = '[' || (
                    SELECT "Id"
                    FROM "Companies"
                    WHERE "Companies"."Name" = "Platforms"."Manufacturer"
                    LIMIT 1
                ) || ']'
                WHERE "Manufacturer" IS NOT NULL
                  AND EXISTS (
                      SELECT 1
                      FROM "Companies"
                      WHERE "Companies"."Name" = "Platforms"."Manufacturer"
                  );
                """);

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Platforms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Platforms"
                SET "Manufacturer" = (
                    SELECT "Name"
                    FROM "Companies"
                    WHERE "Companies"."Id" = CAST(
                        trim(trim("Platforms"."ManufacturerIds", '['), ']')
                        AS INTEGER
                    )
                    LIMIT 1
                )
                WHERE "ManufacturerIds" NOT IN ('[]', '');
                """);

            migrationBuilder.DropColumn(
                name: "ManufacturerIds",
                table: "Platforms");
        }
    }
}
