using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class Keys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Role ADD PRIMARY KEY(roleId)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
