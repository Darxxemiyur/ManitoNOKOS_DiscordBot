using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manito.Migrations
{
    public partial class RemoverDB_And_Frozing_Currency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                table: "PlayerEconomyDeposits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MessagesToRemove",
                columns: table => new
                {
                    MessageID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ChannelID = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Expiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TimesFailed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesToRemove", x => x.MessageID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagesToRemove");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                table: "PlayerEconomyDeposits");
        }
    }
}
