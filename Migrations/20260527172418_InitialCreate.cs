using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    FoundedDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Cover_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Hero_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Icon_Path = table.Column<string>(type: "TEXT", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    IGDB = table.Column<long>(type: "INTEGER", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    GameType = table.Column<int>(type: "INTEGER", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Cover_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Hero_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Icon_Path = table.Column<string>(type: "TEXT", nullable: true),
                    IGDB = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Cover_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Hero_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Logo_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Icon_Path = table.Column<string>(type: "TEXT", nullable: true),
                    IGDB = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    ProfilePicture_Path = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "GameCompanies",
                columns: table => new
                {
                    GameID = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyID = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCompanies", x => new { x.GameID, x.CompanyID, x.Role });
                    table.ForeignKey(
                        name: "FK_GameCompanies_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameCompanies_Games_GameID",
                        column: x => x.GameID,
                        principalTable: "Games",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "Playthroughs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileID = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GameIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Playthroughs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Playthroughs_UserProfiles_UserProfileID",
                        column: x => x.UserProfileID,
                        principalTable: "UserProfiles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGameCollections",
                columns: table => new
                {
                    UserProfileID = table.Column<int>(type: "INTEGER", nullable: false),
                    GameID = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGameCollections", x => new { x.UserProfileID, x.GameID });
                    table.ForeignKey(
                        name: "FK_UserGameCollections_Games_GameID",
                        column: x => x.GameID,
                        principalTable: "Games",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGameCollections_UserProfiles_UserProfileID",
                        column: x => x.UserProfileID,
                        principalTable: "UserProfiles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserGamePlatformOwnerships",
                columns: table => new
                {
                    UserProfileID = table.Column<int>(type: "INTEGER", nullable: false),
                    GameID = table.Column<int>(type: "INTEGER", nullable: false),
                    PlatformID = table.Column<int>(type: "INTEGER", nullable: false),
                    Ownership = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGamePlatformOwnerships", x => new { x.UserProfileID, x.GameID, x.PlatformID, x.Ownership });
                    table.ForeignKey(
                        name: "FK_UserGamePlatformOwnerships_Games_GameID",
                        column: x => x.GameID,
                        principalTable: "Games",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGamePlatformOwnerships_Platforms_PlatformID",
                        column: x => x.PlatformID,
                        principalTable: "Platforms",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGamePlatformOwnerships_UserProfiles_UserProfileID",
                        column: x => x.UserProfileID,
                        principalTable: "UserProfiles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPlatformCollections",
                columns: table => new
                {
                    UserProfileID = table.Column<int>(type: "INTEGER", nullable: false),
                    PlatformID = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPlatformCollections", x => new { x.UserProfileID, x.PlatformID });
                    table.ForeignKey(
                        name: "FK_UserPlatformCollections_Platforms_PlatformID",
                        column: x => x.PlatformID,
                        principalTable: "Platforms",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPlatformCollections_UserProfiles_UserProfileID",
                        column: x => x.UserProfileID,
                        principalTable: "UserProfiles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ID",
                table: "Companies",
                column: "ID",
                unique: true,
                filter: "ID IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GameCompanies_CompanyID",
                table: "GameCompanies",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformCompanies_CompanyID",
                table: "PlatformCompanies",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Platforms_ID",
                table: "Platforms",
                column: "ID",
                unique: true,
                filter: "ID IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_UserProfileID",
                table: "Playthroughs",
                column: "UserProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGameCollections_GameID",
                table: "UserGameCollections",
                column: "GameID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGamePlatformOwnerships_GameID",
                table: "UserGamePlatformOwnerships",
                column: "GameID");

            migrationBuilder.CreateIndex(
                name: "IX_UserGamePlatformOwnerships_PlatformID",
                table: "UserGamePlatformOwnerships",
                column: "PlatformID");

            migrationBuilder.CreateIndex(
                name: "IX_UserPlatformCollections_PlatformID",
                table: "UserPlatformCollections",
                column: "PlatformID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameCompanies");

            migrationBuilder.DropTable(
                name: "PlatformCompanies");

            migrationBuilder.DropTable(
                name: "Playthroughs");

            migrationBuilder.DropTable(
                name: "UserGameCollections");

            migrationBuilder.DropTable(
                name: "UserGamePlatformOwnerships");

            migrationBuilder.DropTable(
                name: "UserPlatformCollections");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Platforms");

            migrationBuilder.DropTable(
                name: "UserProfiles");
        }
    }
}
