using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VGL.Migrations
{
    /// <inheritdoc />
    public partial class UseLocalImagePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cover_ImagePath",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql($"""
                                  UPDATE "Platforms"
                                  SET "ImagePath" = "CoverUrl"
                                  WHERE "CoverUrl" IS NOT NULL
                                        AND "CoverUrl" NOT LIKE 'http://%'
                                        AND "CoverUrl" NOT LIKE 'https://%'
                                        AND "CoverUrl" NOT LIKE '//%';
                                  """);

            migrationBuilder.Sql("""
                                 UPDATE "Companies"
                                 SET "ImagePath" = "CoverUrl"
                                 WHERE "CoverUrl" IS NOT NULL
                                       AND "CoverUrl" NOT LIKE 'http://%'
                                       AND "CoverUrl" NOT LIKE 'https://%'
                                       AND "CoverUrl" NOT LIKE '//%';
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE "Games"
                                 SET "Cover_ImagePath" = "Cover_Url"
                                 WHERE "Cover_Url" IS NOT NULL
                                       AND "Cover_Url" NOT LIKE 'http://%'
                                       AND "Cover_Url" NOT LIKE 'https://%'
                                       AND "Cover_Url" NOT LIKE '//%';
                                 """);

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "CoverUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Cover_Url",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                table: "Platforms",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverUrl",
                table: "Companies",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cover_Url",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                                 UPDATE "Platforms"
                                 SET "CoverUrl" = "ImagePath"
                                 WHERE "ImagePath" IS NOT NULL;
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE "Companies"
                                 SET "CoverUrl" = "ImagePath"
                                 WHERE "ImagePath" IS NOT NULL;
                                 """);

            migrationBuilder.Sql("""
                                 UPDATE "Games"
                                 SET "Cover_Url" = "Cover_ImagePath"
                                 WHERE "Cover_ImagePath" IS NOT NULL;
                                 """);

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Platforms");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Cover_ImagePath",
                table: "Games");
        }
    }
}
