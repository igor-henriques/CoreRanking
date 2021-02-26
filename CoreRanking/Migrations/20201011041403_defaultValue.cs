using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class defaultValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Role ALTER LastIp SET DEFAULT '000000000000';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
