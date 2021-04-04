using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class ADD_K_D : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Death",
                table: "Role",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kill",
                table: "Role",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Death",
                table: "Role");

            migrationBuilder.DropColumn(
                name: "Kill",
                table: "Role");
        }
    }
}
