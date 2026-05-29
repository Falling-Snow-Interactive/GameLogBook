using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLogBook.Migrations
{
    /// <inheritdoc />
    public partial class AddLogbookSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Playthroughs_UserProfileID",
                table: "Playthroughs");

            migrationBuilder.AddColumn<int>(
                name: "GameID",
                table: "Playthroughs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE Playthroughs
                SET GameID = CAST(json_extract(GameIds, '$[0]') AS INTEGER)
                WHERE GameIds IS NOT NULL
                  AND json_valid(GameIds)
                  AND json_array_length(GameIds) > 0
                  AND CAST(json_extract(GameIds, '$[0]') AS INTEGER) IN (SELECT ID FROM Games)
                """);

            migrationBuilder.DropColumn(
                name: "GameIds",
                table: "Playthroughs");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ManualFinishedAt",
                table: "Playthroughs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ManualMasteredAt",
                table: "Playthroughs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ManualStartedAt",
                table: "Playthroughs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlatformID",
                table: "Playthroughs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlaythroughRunID",
                table: "Playthroughs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Playthroughs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameLogs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileID = table.Column<int>(type: "INTEGER", nullable: false),
                    GameID = table.Column<int>(type: "INTEGER", nullable: false),
                    PlaythroughID = table.Column<int>(type: "INTEGER", nullable: false),
                    PlatformID = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    StatusChange = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLogs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_GameLogs_Games_GameID",
                        column: x => x.GameID,
                        principalTable: "Games",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameLogs_Platforms_PlatformID",
                        column: x => x.PlatformID,
                        principalTable: "Platforms",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameLogs_Playthroughs_PlaythroughID",
                        column: x => x.PlaythroughID,
                        principalTable: "Playthroughs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameLogs_UserProfiles_UserProfileID",
                        column: x => x.UserProfileID,
                        principalTable: "UserProfiles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaythroughRuns",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileID = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaythroughRuns", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PlaythroughRuns_UserProfiles_UserProfileID",
                        column: x => x.UserProfileID,
                        principalTable: "UserProfiles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_GameID",
                table: "Playthroughs",
                column: "GameID");

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_PlatformID",
                table: "Playthroughs",
                column: "PlatformID");

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_PlaythroughRunID",
                table: "Playthroughs",
                column: "PlaythroughRunID");

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_UserProfileID_Status",
                table: "Playthroughs",
                columns: new[] { "UserProfileID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GameLogs_GameID",
                table: "GameLogs",
                column: "GameID");

            migrationBuilder.CreateIndex(
                name: "IX_GameLogs_PlatformID",
                table: "GameLogs",
                column: "PlatformID");

            migrationBuilder.CreateIndex(
                name: "IX_GameLogs_PlaythroughID",
                table: "GameLogs",
                column: "PlaythroughID");

            migrationBuilder.CreateIndex(
                name: "IX_GameLogs_UserProfileID_StartedAt",
                table: "GameLogs",
                columns: new[] { "UserProfileID", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaythroughRuns_UserProfileID_Name",
                table: "PlaythroughRuns",
                columns: new[] { "UserProfileID", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Playthroughs_Games_GameID",
                table: "Playthroughs",
                column: "GameID",
                principalTable: "Games",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Playthroughs_Platforms_PlatformID",
                table: "Playthroughs",
                column: "PlatformID",
                principalTable: "Platforms",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Playthroughs_PlaythroughRuns_PlaythroughRunID",
                table: "Playthroughs",
                column: "PlaythroughRunID",
                principalTable: "PlaythroughRuns",
                principalColumn: "ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Playthroughs_Games_GameID",
                table: "Playthroughs");

            migrationBuilder.DropForeignKey(
                name: "FK_Playthroughs_Platforms_PlatformID",
                table: "Playthroughs");

            migrationBuilder.DropForeignKey(
                name: "FK_Playthroughs_PlaythroughRuns_PlaythroughRunID",
                table: "Playthroughs");

            migrationBuilder.DropTable(
                name: "GameLogs");

            migrationBuilder.DropTable(
                name: "PlaythroughRuns");

            migrationBuilder.DropIndex(
                name: "IX_Playthroughs_GameID",
                table: "Playthroughs");

            migrationBuilder.DropIndex(
                name: "IX_Playthroughs_PlatformID",
                table: "Playthroughs");

            migrationBuilder.DropIndex(
                name: "IX_Playthroughs_PlaythroughRunID",
                table: "Playthroughs");

            migrationBuilder.DropIndex(
                name: "IX_Playthroughs_UserProfileID_Status",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "GameID",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "ManualFinishedAt",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "ManualMasteredAt",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "ManualStartedAt",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "PlatformID",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "PlaythroughRunID",
                table: "Playthroughs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Playthroughs");

            migrationBuilder.AddColumn<string>(
                name: "GameIds",
                table: "Playthroughs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Playthroughs_UserProfileID",
                table: "Playthroughs",
                column: "UserProfileID");
        }
    }
}
