using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manito.Migrations
{
    public partial class BigBossThingy001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LoggedTime",
                table: "LogLines",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoggedTime",
                table: "LogLines");
        }
    }
}
