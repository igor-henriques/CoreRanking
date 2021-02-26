using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class fixtable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Role_Account_AccountId",
                table: "Role");

            migrationBuilder.DropIndex(
                name: "IX_Role_AccountId",
                table: "Role");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Role_AccountId",
                table: "Role",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Role_Account_AccountId",
                table: "Role",
                column: "AccountId",
                principalTable: "Account",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
