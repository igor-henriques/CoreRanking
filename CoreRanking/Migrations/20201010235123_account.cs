using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class account : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accountId",
                table: "Role");

            migrationBuilder.AddColumn<int>(
                name: "account",
                table: "Role",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "account",
                table: "Role");

            migrationBuilder.AddColumn<int>(
                name: "accountId",
                table: "Role",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
