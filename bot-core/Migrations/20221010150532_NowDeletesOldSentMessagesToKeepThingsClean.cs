using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Manito.Migrations
{
    public partial class NowDeletesOldSentMessagesToKeepThingsClean : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastStartId",
                table: "MessagesToRemove",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastStartId",
                table: "MessagesToRemove");
        }
    }
}
