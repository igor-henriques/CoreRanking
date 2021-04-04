using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CoreRanking.Migrations
{
    public partial class INITIAL : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Login = table.Column<string>(type: "longtext", nullable: true),
                    Ip = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharacterName = table.Column<string>(type: "longtext", nullable: true),
                    CharacterClass = table.Column<string>(type: "longtext", nullable: true),
                    CharacterGender = table.Column<string>(type: "longtext", nullable: true),
                    Elo = table.Column<string>(type: "longtext", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Doublekill = table.Column<int>(type: "int", nullable: false),
                    Triplekill = table.Column<int>(type: "int", nullable: false),
                    Quadrakill = table.Column<int>(type: "int", nullable: false),
                    Pentakill = table.Column<int>(type: "int", nullable: false),
                    CollectPoint = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_Role_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Banned",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    BanTime = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banned", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Banned_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Battle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    KillerId = table.Column<int>(type: "int", nullable: false),
                    KilledId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Battle_Role_KilledId",
                        column: x => x.KilledId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Battle_Role_KillerId",
                        column: x => x.KillerId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Collect",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collect_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hunt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hunt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hunt_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Role",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Banned_RoleId",
                table: "Banned",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Battle_KilledId",
                table: "Battle",
                column: "KilledId");

            migrationBuilder.CreateIndex(
                name: "IX_Battle_KillerId",
                table: "Battle",
                column: "KillerId");

            migrationBuilder.CreateIndex(
                name: "IX_Collect_RoleId",
                table: "Collect",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Hunt_RoleId",
                table: "Hunt",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_AccountId",
                table: "Role",
                column: "AccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Banned");

            migrationBuilder.DropTable(
                name: "Battle");

            migrationBuilder.DropTable(
                name: "Collect");

            migrationBuilder.DropTable(
                name: "Hunt");

            migrationBuilder.DropTable(
                name: "Role");

            migrationBuilder.DropTable(
                name: "Account");
        }
    }
}
