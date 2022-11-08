using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Manito.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogLines",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    District = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogLines", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "MessageWalls",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WallName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageWalls", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEconomyDeposits",
                columns: table => new
                {
                    DiscordID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ScalesCurr = table.Column<long>(type: "bigint", nullable: false),
                    ChupatCurr = table.Column<long>(type: "bigint", nullable: false),
                    DonatCurr = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEconomyDeposits", x => x.DiscordID);
                });

            migrationBuilder.CreateTable(
                name: "PlayerWorks",
                columns: table => new
                {
                    DiscordID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LastWork = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimesWorked = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerWorks", x => x.DiscordID);
                });

            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "MessageWallLines",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageWallID = table.Column<long>(type: "bigint", nullable: true),
                    WallLine = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageWallLines", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MessageWallLines_MessageWalls_MessageWallID",
                        column: x => x.MessageWallID,
                        principalTable: "MessageWalls",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "MessageWallTranslators",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageWallID = table.Column<long>(type: "bigint", nullable: true),
                    ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CTranslation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageWallTranslators", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MessageWallTranslators_MessageWalls_MessageWallID",
                        column: x => x.MessageWallID,
                        principalTable: "MessageWalls",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageWallLines_MessageWallID",
                table: "MessageWallLines",
                column: "MessageWallID");

            migrationBuilder.CreateIndex(
                name: "IX_MessageWallTranslators_MessageWallID",
                table: "MessageWallTranslators",
                column: "MessageWallID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogLines");

            migrationBuilder.DropTable(
                name: "MessageWallLines");

            migrationBuilder.DropTable(
                name: "MessageWallTranslators");

            migrationBuilder.DropTable(
                name: "PlayerEconomyDeposits");

            migrationBuilder.DropTable(
                name: "PlayerWorks");

            migrationBuilder.DropTable(
                name: "ShopItems");

            migrationBuilder.DropTable(
                name: "MessageWalls");
        }
    }
}
