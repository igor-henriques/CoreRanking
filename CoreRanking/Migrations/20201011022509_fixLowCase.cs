using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class fixLowCase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN points Points int(11);");
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN level Level int(11);");
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN lastIp LastIp longtext;");
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN evento Evento longtext;");
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN characterName CharacterName longtext;");
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN characterClass CharacterClass longtext;");
            migrationBuilder.Sql("ALTER TABLE Role CHANGE COLUMN roleId RoleId int(11);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Points",
                table: "Role",
                newName: "points");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Role",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "LastIp",
                table: "Role",
                newName: "lastIp");

            migrationBuilder.RenameColumn(
                name: "Evento",
                table: "Role",
                newName: "evento");

            migrationBuilder.RenameColumn(
                name: "CharacterName",
                table: "Role",
                newName: "characterName");

            migrationBuilder.RenameColumn(
                name: "CharacterClass",
                table: "Role",
                newName: "characterClass");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "Role",
                newName: "roleId");
        }
    }
}
